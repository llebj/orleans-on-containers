using Client.Options;
using GrainInterfaces;
using Microsoft.Extensions.Options;

namespace Client.Services;

public class GrainObserverManager : IGrainObserverManager
{
    private readonly IClusterClient _clusterClient;
    private readonly IGrainFactory _grainFactory;
    private readonly ObserverManagerOptions _options;
    private readonly TimeProvider _timeProvider;

    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isSubscribed = false;
    private string? _grainId;
    private PeriodicTimer? _periodicTimer;
    private IChatObserver? _reference;
    private Task? _timerTask;

    public GrainObserverManager(
        IClusterClient clusterClient,
        IGrainFactory grainFactory,
        IOptions<ObserverManagerOptions> options,
        TimeProvider timeProvider)
    {
        _clusterClient = clusterClient;
        _grainFactory = grainFactory;
        _options = options.Value;
        _timeProvider = timeProvider;
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

        _cancellationTokenSource = new CancellationTokenSource();
        _periodicTimer = new PeriodicTimer(
            TimeSpan.FromSeconds(_options.RefreshPeriod), 
            _timeProvider);
        _timerTask = Resubscribe(_cancellationTokenSource.Token);

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

        _cancellationTokenSource.Cancel();
        _periodicTimer.Dispose();
        _cancellationTokenSource.Dispose();

        _isSubscribed = false;
    }

    private async Task Resubscribe(CancellationToken cancellationToken)
    {
        try
        {
            while (await _periodicTimer.WaitForNextTickAsync(cancellationToken))
            {
                await Subscribe();
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task Subscribe()
    {
        var grain = _clusterClient.GetGrain<IChatGrain>(_grainId);
        await grain.Subscribe(_reference);
    }
}
