namespace GrainInterfaces;

public interface IChatGrain : IGrainWithIntegerKey
{
    Task SendMessage(Guid clientId, string message);

    Task Subscribe(IChatObserver observer);

    Task Unsubscribe(IChatObserver observer);
}
