namespace GrainInterfaces;

public interface IChatGrain : IGrainWithStringKey
{
    Task SendMessage(string clientId, string message);

    // Observers are unreliable and therefore require regular re-subscription.
    // If this is done with the Subscribe method then if another client attmepts
    // to subscribe using the same Id, then the original subscriber will have
    // lost their connection.
    // TODO: Add a RenewSubscription method to allow subscribed clients to renew
    // their subscriptions.
    Task Subscribe(string clientId, IChatObserver observer);

    Task Unsubscribe(string clientId);
}
