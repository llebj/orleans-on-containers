using GrainInterfaces;
using Shared.Messages;

namespace Grains;

public class ChatGrain : Grain, IChatGrain
{
    private IList<IChatObserver> _observers = new List<IChatObserver>();

    public async Task SendMessage(string clientId, string message)
    {
        var chatMessage = new ChatMessage(this.GetPrimaryKeyString(), clientId, message);
        var tasks = new List<Task>();

        foreach (var observer in _observers)
        {
            tasks.Add(observer.ReceiveMessage(chatMessage));
        }

        await Task.WhenAll(tasks);
    }

    public Task Subscribe(IChatObserver observer)
    {
        _observers.Add(observer);

        return Task.CompletedTask;
    }

    public Task Unsubscribe(IChatObserver observer)
    {
        throw new NotImplementedException();
    }
}
