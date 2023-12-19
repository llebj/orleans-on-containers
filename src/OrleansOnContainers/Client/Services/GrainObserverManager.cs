using GrainInterfaces;

namespace Client.Services;

public class GrainObserverManager : IGrainObserverManager
{
    private readonly IClusterClient _clusterClient;
    private readonly IGrainFactory _grainFactory;
    private readonly IResubscriber<GrainObserverManagerState> _resubscriber;
    private GrainObserverManagerState _state = new();

    public GrainObserverManager(
        IClusterClient clusterClient,
        IGrainFactory grainFactory,
        IResubscriber<GrainObserverManagerState> resubscriber)
    {
        _clusterClient = clusterClient;
        _grainFactory = grainFactory;
        _resubscriber = resubscriber;
    }

    public async Task Subscribe(IChatObserver observer, string grainId)
    {
        if (_state.IsSubscribed)
        {
            var message = "A subscription is already being managed. Unsubscribe first before registering a new subscription.";

            // Return a Task<Result> from this method rather than throwing an exception.
            throw new InvalidOperationException(message);
        }

        _state.Set(grainId, _grainFactory.CreateObjectReference<IChatObserver>(observer));

        try
        {
            await Subscribe(_state);
        }
        catch
        {
            _state.Clear();

            throw;
        }
    }

    public async Task Unsubscribe(IChatObserver observer, string grainId)
    {
        if (!_state.IsSubscribed)
        {
            // Return a Task<Result> from this method rather than throwing an exception.
            throw new InvalidOperationException("No subscription currently exists.");
        }

        var grain = _clusterClient.GetGrain<IChatGrain>(_state.GrainId);
        // _state.Reference cannot be null here if _state.IsSubscribed is true
        await grain.Unsubscribe(_state.Reference!);
        _state.Clear();
        await _resubscriber.Clear();
    }

    private async Task Subscribe(GrainSubscription grainSubscription)
    {
        var grain = _clusterClient.GetGrain<IChatGrain>(grainSubscription.GrainId);
        await grain.Subscribe(grainSubscription.Reference!);
    }
}

public class GrainObserverManagerState : GrainSubscription
{
    public bool IsSubscribed =>
        !string.IsNullOrEmpty(GrainId) &&
        Reference != null;

    public void Clear()
    {
        GrainId = null;
        Reference = null;
    }

    public void Set(string grainId, IChatObserver reference)
    {
        GrainId = grainId;
        Reference = reference;
    }
}
