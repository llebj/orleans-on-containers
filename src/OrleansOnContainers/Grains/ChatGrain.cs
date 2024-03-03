using GrainInterfaces;
using Shared.Messages;

namespace Grains;

public class ChatGrain : Grain, IChatGrain
{
    private readonly Dictionary<string, IChatObserver> _observers = [];

    /// <summary>
    /// Sends a message to all subscribed clients.
    /// </summary>
    /// <param name="clientId">The client sending the message.</param>
    /// <param name="message">The message to be sent.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">Thrown when the sending client is not subscribed.</exception>
    public async Task SendMessage(string clientId, string message)
    {
        var grainId = this.GetPrimaryKeyString();

        if (!_observers.ContainsKey(clientId))
        {
            throw new InvalidOperationException($"The client \"{clientId}\" attempted to send a message to the chat \"{grainId}\" without an active subscription.");
        }

        var chatMessage = new ChatMessage(grainId, clientId, message);
        await NotifyObservers(chatMessage);
    }

    public async Task Subscribe(string clientId, IChatObserver observer)
    {
        var isNewSubscriber = _observers.TryAdd(clientId, observer);

        if (!isNewSubscriber)
        {
            _observers[clientId] = observer;

            return;
        }

        var message = new SubscriptionMessage(this.GetPrimaryKeyString(), clientId);
        await NotifyObservers(message, observerId => observerId != clientId);
    }

    public async Task Unsubscribe(string clientId)
    {
        if (!_observers.ContainsKey(clientId))
        {
            return;
        }

        _observers.Remove(clientId);
        var message = new UnsubscriptionMessage(this.GetPrimaryKeyString(), clientId);
        await NotifyObservers(message);
    }

    private async Task NotifyObservers(IMessage message, Func<string, bool>? predicate = null)
    {
        var i = 0;
        var clients = new string[_observers.Count];
        var tasks = new Task[_observers.Count];
        var failedClients = new List<string>();

        foreach (var observer in _observers)
        {
            if (predicate is not null && !predicate(observer.Key))
            {
                continue;
            }

            clients[i] = observer.Key;
            tasks[i] = observer.Value.ReceiveMessage(message);
            ++i;
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            failedClients = ProcessFailedTasks(clients, tasks, i);
        }

        foreach (string client in failedClients) 
        { 
            _observers.Remove(client);
        }
    }

    private List<string> ProcessFailedTasks(string[] clients, Task[] tasks, int limit)
    {
        var result = new List<string>();
        // Limit should never be greater than the array lengths, but to be sure we want
        // to adjust limit.
        limit = Math.Min(clients.Length, limit);
        limit = Math.Min(tasks.Length, limit);

        for (int i = 0; i < limit; i++)
        {
            Task task = tasks[i];

            if (task.IsFaulted)
            {
                result.Add(clients[i]);
            }
        }

        return result;
    }
}
