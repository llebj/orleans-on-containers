namespace GrainInterfaces;

public interface IChatGrain : IGrainWithIntegerKey
{
    Task SendMessage(Guid clientId, string message);

    Task Subscribe(IChat observer);

    Task Unsubscribe(IChat observer);
}
