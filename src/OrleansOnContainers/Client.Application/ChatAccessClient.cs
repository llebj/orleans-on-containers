using Client.Application.Contracts;
using GrainInterfaces;
using Shared.Messages;

namespace Client.Application;

internal class ChatAccessClient : IChatAccessClient
{
    private readonly IGrainFactory _grainFactory;
    private readonly IMessageStreamWriterAllocator _messageStreamWriterAllocator;
    private readonly IResubscriberManager _resubscriberManager;
    private ChatObserver? _observer;

    public ChatAccessClient(
        IGrainFactory grainFactory,
        IMessageStreamWriterAllocator messageStreamWriterAllocator,
        IResubscriberManager resubscriberManager)
    {
        _grainFactory = grainFactory;
        _messageStreamWriterAllocator = messageStreamWriterAllocator;
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

        var (Writer, ReleaseKey) = _messageStreamWriterAllocator.GetWriter();
        _observer = new ChatObserver(Writer);
        var observerReference = _grainFactory.CreateObjectReference<IChatObserver>(_observer);
        await grainReference.Subscribe(clientId, screenName, observerReference);

        await _resubscriberManager.StartResubscribing(grainReference, clientId, observerReference);
        
        return Result.Success();
    }
     
    public async Task<Result> LeaveChat(string chat, Guid clientId)
    {
        var grainReference = _grainFactory.GetGrain<IChatGrain>(chat);
        await grainReference.Unsubscribe(clientId);
        _observer = null;

        await _resubscriberManager.StopResubscribing();

        return Result.Success();
    }
}

internal class ChatObserver(MessageStreamWriter writer) : IChatObserver
{
    private readonly MessageStreamWriter _writer = writer;

    public async Task ReceiveMessage(IMessage message) => await _writer.WriteMessage(message, default);
}
