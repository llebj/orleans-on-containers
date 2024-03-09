using GrainInterfaces;
using Grains.Options;
using Microsoft.Extensions.Options;
using Shared.Messages;
using System.Collections;

namespace Grains;

public class ChatGrain : Grain, IChatGrain
{
    private readonly SubscriberManager _subscriberManager = new();
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

        if (!_subscriberManager.ClientIsSubscribed(clientId))
        {
            throw new InvalidOperationException($"The client \"{clientId}\" attempted to send a message to the chat \"{grainId}\" without an active subscription.");
        }

        var chatMessage = new ChatMessage(grainId, _subscriberManager.GetClientScreenName(clientId), message);
        await NotifyObservers(chatMessage);
    }

    public async Task Subscribe(Guid clientId, string screenName, IChatObserver observer)
    {
        var isNewSubscriber = _subscriberManager.AddSubscriber(clientId, screenName, GetCurrentTime(), observer);

        if (!isNewSubscriber)
        {
            return;
        }

        var message = new SubscriptionMessage(this.GetPrimaryKeyString(), screenName);
        await NotifyObservers(message, observerId => observerId != clientId);
    }

    public async Task Unsubscribe(Guid clientId)
    {
        if (!_subscriberManager.ClientIsSubscribed(clientId))
        {
            return;
        }

        var subscriberState = _subscriberManager.RemoveSubscriber(clientId);
        var message = new UnsubscriptionMessage(this.GetPrimaryKeyString(), subscriberState.ScreenName);
        await NotifyObservers(message);
    }

    private DateTimeOffset GetCurrentTime() => _timeProvider.GetUtcNow();

    private async Task NotifyObservers(IMessage message, Func<Guid, bool>? predicate = null)
    {
        var currentTime = GetCurrentTime();
        var i = 0;
        var clients = new Guid[_subscriberManager.Count];
        var tasks = new Task[_subscriberManager.Count];
        var failedClients = new HashSet<Guid>();

        foreach (var (Id, Subscriber) in _subscriberManager)
        {
            if ((currentTime - Subscriber.LastSeen) > _observerTimeout)
            {
                failedClients.Add(Id);

                continue;
            }

            if (predicate is not null && !predicate(Id))
            {
                continue;
            }

            clients[i] = Id;
            tasks[i] = Subscriber.Observer.ReceiveMessage(message);
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
            _subscriberManager.RemoveSubscriber(client);
        }
    }

    private static HashSet<Guid> ProcessFailedTasks(HashSet<Guid> failedClients, Guid[] clients, Task[] tasks, int limit)
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
}

internal class SubscriberManager : IEnumerable<KeyValuePair<Guid, SubscriberState>>
{
    private readonly Dictionary<Guid, SubscriberState> _subscribers = [];

    public int Count => _subscribers.Count;

    public bool AddSubscriber(Guid clientId, string screenName, DateTimeOffset currentTime, IChatObserver observer)
    {
        var observerState = new SubscriberState(screenName, currentTime, observer);
        var isNewSubscriber = _subscribers.TryAdd(clientId, observerState);

        if (!isNewSubscriber)
        {
            _subscribers[clientId] = observerState;
        }

        return isNewSubscriber;
    }

    public bool ClientIsSubscribed(Guid clientId) => _subscribers.ContainsKey(clientId);

    public string GetClientScreenName(Guid clientId) => _subscribers[clientId].ScreenName;

    public IEnumerator<KeyValuePair<Guid, SubscriberState>> GetEnumerator() => _subscribers.GetEnumerator();

    public SubscriberState RemoveSubscriber(Guid clientId)
    {
        _subscribers.Remove(clientId, out var state);

        return state!;
    }

    IEnumerator IEnumerable.GetEnumerator() => _subscribers.GetEnumerator();
}

internal class SubscriberState(string ScreenName, DateTimeOffset LastSeen, IChatObserver Observer)
{
    public DateTimeOffset LastSeen { get; set; } = LastSeen;

    public IChatObserver Observer { get; set; } = Observer;

    public string ScreenName { get; set; } = ScreenName;
}
