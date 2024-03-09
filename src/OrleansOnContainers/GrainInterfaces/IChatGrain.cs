namespace GrainInterfaces;

/*
 * Using a user-selected string for the clientId was a mistake. The clientId should
 * be a unique (guid) value tied to a particular instance of the client application.
 * The user should then choose a screen name that they wish to be identified as within
 * a particular chat. This screen name should be validated to ensure that it does not
 * currently exist within the chat. The user should have no control over the clientId.
 */

// TODO: Change the 'clientId' parameters back to a guid.
// TODO: Add a RenewSubscription method to allow subscribed clients to renew
//       their subscriptions rather than relying on the Subscribe method for
//       both functions.

/*
 * Grain implementations should be considered "library" code and should provide public
 * methods to validate user input before submitting. Exceptions should then be thrown if
 * the grain is in an invalid state e.g., a client attempts to unsubscribe before they have
 * subscribed - this is exceptional as those order of operations should never occur.
 */
public interface IChatGrain : IGrainWithStringKey
{
    Task SendMessage(Guid clientId, string message);

    // TODO: Allow a user to specify a screen name when subscribing.
    Task Subscribe(Guid clientId, IChatObserver observer);

    Task Unsubscribe(Guid clientId);
}
