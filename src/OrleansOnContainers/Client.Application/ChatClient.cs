using Client.Application.Contracts;
using GrainInterfaces;
using Microsoft.Extensions.Logging;
using Shared.Messages;

namespace Client.Application;

internal class ChatClient : IChatClient
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<ChatClient> _logger;
    private readonly ObserverManager _observerManager = new();

    public ChatClient(
        IGrainFactory grainFactory,
        ILogger<ChatClient> logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
    }

    public async Task<Result> JoinChat(string chat, Guid clientId, string screenName)
    {
        if (_observerManager.IsManagingAnObserver)
        {
            return Result.Failure("You are already subscribed to a chat. Leave the current chat in order to join another.");
        }

        var grainReference = _grainFactory.GetGrain<IChatGrain>(chat);
        var screenNameIsAvailable = await grainReference.ScreenNameIsAvailable(screenName);

        if (!screenNameIsAvailable)
        {
            return Result.Failure($"The screen name '{screenName}' is not available. Please select another one.");
        }

        var observerReference = _observerManager.GetObserverReference(_grainFactory);
        await grainReference.Subscribe(clientId, screenName, observerReference);

        return Result.Success();
    }

    public Task<Result> LeaveCurrentChat(Guid clientId)
    {
        throw new NotImplementedException();
    }

    public Task<Result> SendMessage(Guid clientId, string message)
    {
        throw new NotImplementedException();
    }
}

internal class ObserverManager
{
    private IChatObserver? _observer;

    public bool IsManagingAnObserver => _observer is not null;

    public IChatObserver GetObserverReference(IGrainFactory grainFactory)
    {
        _observer = new ChatObserver();

        return grainFactory.CreateObjectReference<IChatObserver>(_observer);
    }
}

internal class ChatObserver : IChatObserver
{
    public Task ReceiveMessage(IMessage message)
    {
        throw new NotImplementedException();
    }
}
