using GrainInterfaces;

namespace Client.Services;

public interface IGrainObserverManager
{
    Task Subscribe(IChatObserver observer, string grainId);

    Task Unsubscribe(IChatObserver observer, string grainId);
}
