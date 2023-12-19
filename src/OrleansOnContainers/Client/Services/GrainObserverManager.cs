using GrainInterfaces;

namespace Client.Services;

public class GrainObserverManager : IGrainObserverManager
{
    private readonly IClusterClient _clusterClient;
    private readonly IGrainFactory _grainFactory;
    private readonly IPeriodicTimer<GrainObserverManagerState> _periodicTimer;
    private bool _isSubscribed = false;
    private string? _grainId;
    private IChatObserver? _reference;

    public GrainObserverManager(
        IClusterClient clusterClient,
        IGrainFactory grainFactory,
        IPeriodicTimer<GrainObserverManagerState> periodicTimer)
    {
        _clusterClient = clusterClient;
        _grainFactory = grainFactory;
        _periodicTimer = periodicTimer;
    }

    public async Task Subscribe(IChatObserver observer, string grainId)
    {
        if (_isSubscribed)
        {
            var message = "A subscription is already being managed. Unsubscribe first before registering a new subscription";

            throw new InvalidOperationException(message);
        }

        _reference = _grainFactory.CreateObjectReference<IChatObserver>(observer);
        _grainId = grainId;

        await Subscribe();

        //_cancellationTokenSource = new CancellationTokenSource();
        //_periodicTimer = new PeriodicTimer(
        //    TimeSpan.FromSeconds(_options.RefreshPeriod), 
        //    _timeProvider);
        
        //// The resubscription lolgic needs to be broken out into a separate class
        //// in order to avoid the race condition present in the current set of unit tests.
        //_timerTask = Resubscribe(_cancellationTokenSource.Token);

        _isSubscribed = true;
    }

    public async Task Unsubscribe(IChatObserver observer, string grainId)
    {
        if (!_isSubscribed)
        {
            throw new InvalidOperationException("No subscription currently exists.");
        }

        var grain = _clusterClient.GetGrain<IChatGrain>(_grainId);
        await grain.Unsubscribe(_reference);
        await _periodicTimer.Stop();

        //_cancellationTokenSource.Cancel();
        //_periodicTimer.Dispose();
        //_cancellationTokenSource.Dispose();

        _isSubscribed = false;
    }

    private async Task Subscribe()
    {
        var grain = _clusterClient.GetGrain<IChatGrain>(_grainId);
        await grain.Subscribe(_reference);
    }
}

public class GrainObserverManagerState
{

}
