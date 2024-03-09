using GrainInterfaces;
using Grains.Options;
using Microsoft.Extensions.Options;
using Shared.Messages;

namespace Grains;

public class ChatGrain : Grain, IChatGrain
{
    private readonly Dictionary<Guid, ObserverState> _observers = [];
    private readonly TimeSpan _observerTimeout;
    private readonly TimeProvider _timeProvider;

    public ChatGrain(
        IOptions<ChatGrainOptions> options,
        TimeProvider timeProvider)
    {
        _observerTimeout = TimeSpan.FromSeconds(options.Value.ObserverTimeout);
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Sends a message to all subscribed clients.
    /// </summary>
    /// <param name="clientId">The client sending the message.</param>
    /// <param name="message">The message to be sent.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">Thrown when the sending client is not subscribed.</exception>
    public async Task SendMessage(Guid clientId, string message)
    {
        var grainId = this.GetPrimaryKeyString();

        if (!_observers.ContainsKey(clientId))
        {
            throw new InvalidOperationException($"The client \"{clientId}\" attempted to send a message to the chat \"{grainId}\" without an active subscription.");
        }

        var chatMessage = new ChatMessage(grainId, clientId, message);
        await NotifyObservers(chatMessage);
    }

    public async Task Subscribe(Guid clientId, IChatObserver observer)
    {
        var observerState = new ObserverState(GetCurrentTime(), observer);
        var isNewSubscriber = _observers.TryAdd(clientId, observerState);

        if (!isNewSubscriber)
        {
            _observers[clientId] = observerState;

            return;
        }

        var message = new SubscriptionMessage(this.GetPrimaryKeyString(), clientId);
        await NotifyObservers(message, observerId => observerId != clientId);
    }

    public async Task Unsubscribe(Guid clientId)
    {
        if (!_observers.ContainsKey(clientId))
        {
            return;
        }

        _observers.Remove(clientId);
        var message = new UnsubscriptionMessage(this.GetPrimaryKeyString(), clientId);
        await NotifyObservers(message);
    }

    private DateTimeOffset GetCurrentTime() => _timeProvider.GetUtcNow();

    private async Task NotifyObservers(IMessage message, Func<Guid, bool>? predicate = null)
    {
        var currentTime = GetCurrentTime();
        var i = 0;
        var clients = new Guid[_observers.Count];
        var tasks = new Task[_observers.Count];
        var failedClients = new HashSet<Guid>();

        foreach (var (Key, Observer) in _observers)
        {
            if ((currentTime - Observer.LastSeen) > _observerTimeout)
            {
                failedClients.Add(Key);

                continue;
            }

            if (predicate is not null && !predicate(Key))
            {
                continue;
            }

            clients[i] = Key;
            tasks[i] = Observer.Observer.ReceiveMessage(message);
            ++i;
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            failedClients = ProcessFailedTasks(failedClients, clients, tasks, i);
        }

        foreach (var client in failedClients) 
        { 
            _observers.Remove(client);
        }
    }

    private HashSet<Guid> ProcessFailedTasks(HashSet<Guid> failedClients, Guid[] clients, Task[] tasks, int limit)
    {
        // Limit should never be greater than the array lengths, but to be sure we want
        // to adjust limit.
        limit = Math.Min(clients.Length, limit);
        limit = Math.Min(tasks.Length, limit);

        for (int i = 0; i < limit; i++)
        {
            Task task = tasks[i];

            if (task.IsFaulted)
            {
                failedClients.Add(clients[i]);
            }
        }

        return failedClients;
    }

    private class ObserverState(DateTimeOffset LastSeen, IChatObserver Observer)
    {
        public DateTimeOffset LastSeen { get; set; } = LastSeen;

        public IChatObserver Observer { get; set; } = Observer;
    }
}
