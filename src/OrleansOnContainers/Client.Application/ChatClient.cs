using Client.Application.Contracts;
using GrainInterfaces;
using Microsoft.Extensions.Logging;
using Shared.Messages;
using System.Threading.Channels;

namespace Client.Application;

internal class ChatClient : IChatClient
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<ChatClient> _logger;
    private readonly IMessageStream _messageStream;
    private ChatObserver? _observer;

    public ChatClient(
        IGrainFactory grainFactory,
        ILogger<ChatClient> logger,
        IMessageStream messageStream)
    {
        _grainFactory = grainFactory;
        _logger = logger;
        _messageStream = messageStream;
    }

    public async Task<Result> JoinChat(string chat, Guid clientId, string screenName)
    {
        var grainReference = _grainFactory.GetGrain<IChatGrain>(chat);
        var screenNameIsAvailable = await grainReference.ScreenNameIsAvailable(screenName);

        if (!screenNameIsAvailable)
        {
            return Result.Failure($"The screen name '{screenName}' is not available. Please select another one.");
        }

        _observer = new ChatObserver(_messageStream.GetWriter());
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

internal class ChatObserver(ChannelWriter<IMessage> writer) : IChatObserver
{
    private readonly ChannelWriter<IMessage> _writer = writer;

    public async Task ReceiveMessage(IMessage message) => await _writer.WriteAsync(message);
}
