using GrainInterfaces;
using Shared.Messages;

namespace Grains;

public class ChatGrain : Grain, IChatGrain
{
    private readonly IDictionary<string, IChatObserver> _observers = new Dictionary<string, IChatObserver>();

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

    public Task Unsubscribe(IChatObserver observer)
    {
        throw new NotImplementedException();
    }

    private async Task NotifyObservers(IMessage message, Func<string, bool>? predicate = null)
    {
        var tasks = new List<Task>();

        foreach (var observer in _observers)
        {
            if (predicate is not null && !predicate(observer.Key))
            {
                continue;
            }

            tasks.Add(observer.Value.ReceiveMessage(message));
        }

        await Task.WhenAll(tasks);
    }
}
