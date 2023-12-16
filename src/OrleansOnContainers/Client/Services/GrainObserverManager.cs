using GrainInterfaces;

namespace Client.Services;

public class GrainObserverManager : IGrainObserverManager
{
    private readonly IClusterClient _clusterClient;
    private bool _isSubscribed = false;

    public GrainObserverManager(
        IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task Subscribe(IChatObserver observer, string grainId)
    {
        if (_isSubscribed)
        {
            var message = "A subscription is already being managed. Unsubscribe first before registering a new subscription";

            throw new InvalidOperationException(message);
        }

        var grain = _clusterClient.GetGrain<IChatGrain>(grainId);
        await grain.Subscribe(observer);
        _isSubscribed = true;
    }

    public Task Unsubscribe(IChatObserver observer, string grainId)
    {
        throw new NotImplementedException();
    }
}
