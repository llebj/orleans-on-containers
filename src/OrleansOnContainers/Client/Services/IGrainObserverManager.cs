using GrainInterfaces;

namespace Client.Services;

internal interface IGrainObserverManager
{
    Task Subscribe(IChatObserver observer, int grainId);

    Task Unsubscribe(IChatObserver observer, int grainId);
}
