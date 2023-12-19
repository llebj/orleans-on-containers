using GrainInterfaces;

namespace Client.Services;

// The methods on this interface probably want to return a Task<Result>
public interface IGrainObserverManager
{
    Task Subscribe(IChatObserver observer, string grainId);

    Task Unsubscribe(IChatObserver observer, string grainId);
}
