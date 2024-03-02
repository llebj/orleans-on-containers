namespace GrainInterfaces;

public interface IChatGrain : IGrainWithStringKey
{
    Task SendMessage(string clientId, string message);

    Task Subscribe(string clientId, IChatObserver observer);

    Task Unsubscribe(IChatObserver observer);
}
