using Client.Application.Contracts;
using GrainInterfaces;
using Microsoft.Extensions.Logging;
using Shared.Messages;

namespace Client.Application;

public class ChatClient : IChatClient
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<ChatClient> _logger;
    private readonly IMessageStreamWriterAllocator _messageStreamWriterAllocator;
    private ChatObserver? _observer;

    public ChatClient(
        IGrainFactory grainFactory,
        ILogger<ChatClient> logger,
        IMessageStreamWriterAllocator messageStreamWriterAllocator)
    {
        _grainFactory = grainFactory;
        _logger = logger;
        _messageStreamWriterAllocator = messageStreamWriterAllocator;
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

        return Result.Success();
    }

    public async Task<Result> LeaveChat(string chat, Guid clientId)
    {
        var grainReference = _grainFactory.GetGrain<IChatGrain>(chat);
        await grainReference.Unsubscribe(clientId);
        _observer = null;

        return Result.Success();
    }

    public async Task<Result> SendMessage(string chat, Guid clientId, string message)
    {
        var grainReference = _grainFactory.GetGrain<IChatGrain>(chat);
        await grainReference.SendMessage(clientId, message);

        return Result.Success();
    }
}

internal class ChatObserver(MessageStreamWriter writer) : IChatObserver
{
    private readonly MessageStreamWriter _writer = writer;

    public async Task ReceiveMessage(IMessage message) => await _writer.WriteMessage(message, default);
}
