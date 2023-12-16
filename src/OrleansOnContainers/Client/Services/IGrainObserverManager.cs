using GrainInterfaces;

namespace Client.Services;

internal interface IGrainObserverManager
{
    Task Subscribe(IChatObserver observer, string grainId);

    Task Unsubscribe(IChatObserver observer, string grainId);
}
