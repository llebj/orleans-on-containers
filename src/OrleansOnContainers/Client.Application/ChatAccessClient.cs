using Client.Application.Contracts;
using GrainInterfaces;

namespace Client.Application;

internal class ChatAccessClient : IChatAccessClient
{
    private readonly IGrainFactory _grainFactory;
    private readonly IObserverManager _observerManager;
    private readonly IResubscriberManager _resubscriberManager;

    public ChatAccessClient(
        IGrainFactory grainFactory,
        IObserverManager observerManager,
        IResubscriberManager resubscriberManager)
    {
        _grainFactory = grainFactory;
        _observerManager = observerManager;
        _resubscriberManager = resubscriberManager;
    }

    public async Task<Result> JoinChat(string chat, Guid clientId, string screenName)
    {
        var grainReference = _grainFactory.GetGrain<IChatGrain>(chat);
        var screenNameIsAvailable = await grainReference.ScreenNameIsAvailable(screenName);

        if (!screenNameIsAvailable)
        {
            return Result.Failure($"The screen name '{screenName}' is not available. Please select another one.");
        }

        var observer = _observerManager.CreateObserver();
        var observerReference = _grainFactory.CreateObjectReference<IChatObserver>(observer);
        await grainReference.Subscribe(clientId, screenName, observerReference);
        await _resubscriberManager.StartResubscribing(grainReference, clientId, observerReference);

        return Result.Success();
    }
     
    public async Task<Result> LeaveChat(string chat, Guid clientId)
    {
        var grainReference = _grainFactory.GetGrain<IChatGrain>(chat);
        await grainReference.Unsubscribe(clientId);
        _observerManager.DestroyObserver();
        await _resubscriberManager.StopResubscribing();

        return Result.Success();
    }
}
