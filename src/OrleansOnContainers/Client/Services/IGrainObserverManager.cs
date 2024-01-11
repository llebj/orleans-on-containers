using GrainInterfaces;

namespace Client.Services;

// TODO: There is no reason that this interface needs to be bound to observers.
//       It could be changed to be an ISubscriptionManager and still server the
//       same purpose.
public interface IGrainObserverManager
{
    Task<Result> Subscribe(IChatObserver observer, string grainId);

    Task<Result> Unsubscribe(IChatObserver observer, string grainId);
}
