namespace GrainInterfaces;

public interface IOldChatGrain : IGrainWithStringKey
{
    Task SendMessage(Guid clientId, string message);

    Task Subscribe(IChatObserver observer);

    Task Unsubscribe(IChatObserver observer);
}