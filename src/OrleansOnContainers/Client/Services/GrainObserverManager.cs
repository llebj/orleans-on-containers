using GrainInterfaces;

namespace Client.Services;

public class GrainObserverManager : IGrainObserverManager
{
    private readonly IClusterClient _clusterClient;
    private readonly IGrainFactory _grainFactory;
    private readonly IResubscriber<GrainSubscription> _resubscriber;
    private GrainObserverManagerState _state = new();

    public GrainObserverManager(
        IClusterClient clusterClient,
        IGrainFactory grainFactory,
        IResubscriber<GrainSubscription> resubscriber)
    {
        _clusterClient = clusterClient;
        _grainFactory = grainFactory;
        _resubscriber = resubscriber;
    }

    public async Task<Result> Subscribe(IChatObserver observer, string grainId)
    {
        if (_state.IsSubscribed)
        {
            var message = "A subscription is already being managed. Unsubscribe first before registering a new subscription.";

            return Result.Failure(message);
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

        await _resubscriber.Register(_state, Subscribe);

        return Result.Success();
    }

    public async Task<Result> Unsubscribe(IChatObserver observer, string grainId)
    {
        if (!_state.IsSubscribed)
        {
            return Result.Failure("No subscription currently exists.");
        }

        var grain = _clusterClient.GetGrain<IChatGrain>(_state.GrainId);
        // _state.Reference cannot be null here if _state.IsSubscribed is true
        await grain.Unsubscribe(_state.Reference!);
        _state.Clear();
        await _resubscriber.Clear();

        return Result.Success();
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
