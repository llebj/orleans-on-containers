namespace Client.Services;

public interface ISubscriptionManager
{
    Task<Result> Subscribe(string grainId);

    Task<Result> Unsubscribe(string grainId);
}
