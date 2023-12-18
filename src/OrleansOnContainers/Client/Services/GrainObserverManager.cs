using Client.Options;
using GrainInterfaces;
using Microsoft.Extensions.Options;

namespace Client.Services;

public class GrainObserverManager : IGrainObserverManager
{
    private readonly IClusterClient _clusterClient;
    private readonly IOptions<ObserverManagerOptions> _options;
    private bool _isSubscribed = false;

    public GrainObserverManager(
        IClusterClient clusterClient,
        IOptions<ObserverManagerOptions> options)
    {
        _clusterClient = clusterClient;
        _options = options;
    }

    public async Task Subscribe(IChatObserver observer, string grainId)
    {
        if (_isSubscribed)
        {
            var message = "A subscription is already being managed. Unsubscribe first before registering a new subscription";

            throw new InvalidOperationException(message);
        }

        // A new PeriodicTimer wants to be instantiated and set to resubscribe
        // on each timer tick. A cancellation token source wants to be created
        // and passed into the timer, allowing the operation to be cancelled.
        var grain = _clusterClient.GetGrain<IChatGrain>(grainId);
        await grain.Subscribe(observer);
        _isSubscribed = true;
    }

    public Task Unsubscribe(IChatObserver observer, string grainId)
    {
        // When a client unsubscibes, the re-subscribe operation wants to be cancelled
        // using the cancellation token. Immediately afterwards, the existing timer wants
        // to be disposed of and set to null.
        throw new NotImplementedException();
    }
}
