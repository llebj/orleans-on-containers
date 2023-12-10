using GrainInterfaces;

namespace Client.Services;

internal interface IGrainObserverManager
{
    void Subscribe(IChatObserver observer, int grainId);

    void Unsubscribe(IChatObserver observer, int grainId);
}
