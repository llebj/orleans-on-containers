namespace GrainInterfaces;

/*
 * Grain implementations should be considered "library" code and should provide public
 * methods to validate user input before submitting. Exceptions should then be thrown if
 * the grain is in an invalid state e.g., a client attempts to unsubscribe before they have
 * subscribed - this is exceptional as those order of operations should never occur.
 */
public interface IChatGrain : IGrainWithStringKey
{
    Task Resubscribe(Guid clientId, IChatObserver observer);

    Task<bool> ScreenNameIsAvailable(string screenName);

    Task SendMessage(Guid clientId, string message);

    Task Subscribe(Guid clientId, string screenName, IChatObserver observer);

    Task Unsubscribe(Guid clientId);
}
