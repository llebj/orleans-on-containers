using GrainInterfaces;
using Microsoft.Extensions.Logging;

namespace Client.Services;

public class GrainObserverManager : ISubscriptionManager
{
    private const string _generalSubscriptionFailureMessage = "An error occurred during the subscription process."; 
    private readonly IChatObserver _chatObserver;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<GrainObserverManager> _logger;
    private readonly IResubscriber<GrainSubscription> _resubscriber;
    private readonly SubscriptionInformation _subscriptionInformation;

    // This class has a lot of dependencies...
    public GrainObserverManager(
        IChatObserver chatObserver,
        IClusterClient clusterClient,
        IGrainFactory grainFactory,
        ILogger<GrainObserverManager> logger,
        IResubscriber<GrainSubscription> resubscriber)
    {
        _chatObserver = chatObserver;
        _clusterClient = clusterClient;
        _logger = logger;
        _resubscriber = resubscriber;
        _subscriptionInformation = new SubscriptionInformation(grainFactory, logger);
    }

    public async Task<Result> Subscribe(string grainId)
    {
        _logger.LogDebug("Attempting to subscribe to {Grain}.", grainId);

        if (_subscriptionInformation.IsValid)
        {
            _logger.LogDebug("Failed to subscribe to {Grain}. Client is already subscribed to {Grain}.", grainId, _subscriptionInformation.GrainId);

            return Result.Failure("A subscription is already being managed. Unsubscribe first before registering a new subscription.");
        }

        if (!_subscriptionInformation.Build(grainId, _chatObserver) 
            || !await Subscribe(_subscriptionInformation.GrainSubscription!))
        {
            return Result.Failure(_generalSubscriptionFailureMessage);
        }
        
        var registeredResubscriber = await RegisterResubscriber(_subscriptionInformation.GrainSubscription!);

        if (!registeredResubscriber)
        {
            _logger.LogWarning("Resubscriber registration failed; automatic resubscription will not take place.");
        }

        _logger.LogDebug("Successfully subscribed to {Grain}.", grainId);

        return Result.Success(!registeredResubscriber ? 
            "Automatic resubscription will not take place. This action will need to be performed manually." : 
            string.Empty);
    }

    public async Task<Result> Unsubscribe(string grainId)
    {
        _logger.LogDebug("Attempting to subscribe to {Grain}.", grainId);

        if (!_subscriptionInformation.IsValid)
        {
            _logger.LogDebug("Failed to unsubscribe to {Grain}. No existing subscription exists.", grainId);

            return Result.Failure("No subscription currently exists.");
        }

        var grain = _clusterClient.GetGrain<IChatGrain>(_subscriptionInformation.GrainId);
        // GrainSubscription cannot be null here if IsValid
        await grain.Unsubscribe(_subscriptionInformation.GrainSubscription!.ObjectReference);
        _subscriptionInformation.Clear();
        await _resubscriber.Clear();

        _logger.LogDebug("Successfully unsubscribed from {Grain}.", grainId);

        return Result.Success();
    }

    private async Task<bool> RegisterResubscriber(GrainSubscription grainSubscription)
    {
        try
        {
            await _resubscriber.Register(grainSubscription, SubscribeToGrain);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register resubscription delegate for {Grain}.", _subscriptionInformation.GrainId);
        }

        return false;
    }

    private async Task<bool> Subscribe(GrainSubscription grainSubscription)
    {
        try
        {
            await SubscribeToGrain(grainSubscription);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to {Grain}.", _subscriptionInformation.GrainId);
            _subscriptionInformation.Clear();
        }

        return false;
    }

    private async Task SubscribeToGrain(GrainSubscription grainSubscription)
    {
        var grain = _clusterClient.GetGrain<IChatGrain>(grainSubscription.GrainId);
        await grain.Subscribe(grainSubscription.ObjectReference!);
    }
}

internal class SubscriptionInformation
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger _logger;
    private GrainSubscription? _grainSubscription;

    public SubscriptionInformation(
        IGrainFactory grainFactory,
        ILogger logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
    }

    public string GrainId => _grainSubscription?.GrainId ?? string.Empty;

    public bool IsValid => _grainSubscription is not null;

    public GrainSubscription? GrainSubscription => _grainSubscription;

    /// <summary>
    /// Attempts to build an instance of GrainSubscription.
    /// </summary>
    /// <param name="grainId">The id of the grain to subscribe to.</param>
    /// <param name="observer">An instance of IChatObserver to register as a subscriber.</param>
    /// <returns>A boolean signifying whether the operation succeeded or not.</returns>
    public bool Build(string grainId, IChatObserver observer)
    {
        IChatObserver reference;

        try
        {
            reference = _grainFactory.CreateObjectReference<IChatObserver>(observer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create object reference for observer.");

            return false;
        }

        _grainSubscription = new GrainSubscription(grainId, reference);

        return true;
    }

    public void Clear()
    {
        _grainSubscription = null;
    }
}
