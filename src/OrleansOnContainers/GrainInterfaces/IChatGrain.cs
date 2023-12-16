namespace GrainInterfaces;

public interface IChatGrain : IGrainWithStringKey
{
    Task SendMessage(Guid clientId, string message);

    Task Subscribe(IChatObserver observer);

    Task Unsubscribe(IChatObserver observer);
}
