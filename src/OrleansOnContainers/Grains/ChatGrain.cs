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
        var tasks = new List<Task>();

        foreach (var observer in _observers.Values)
        {
            tasks.Add(observer.ReceiveMessage(chatMessage));
        }

        await Task.WhenAll(tasks);
    }

    public Task Subscribe(IChatObserver observer)
    {
        _observers.Add(observer.ToString(), observer);

        return Task.CompletedTask;
    }

    public Task Unsubscribe(IChatObserver observer)
    {
        throw new NotImplementedException();
    }
}
