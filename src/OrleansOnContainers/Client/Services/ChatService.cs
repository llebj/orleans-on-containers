using GrainInterfaces;
using Microsoft.Extensions.Logging;

namespace Client.Services;

public class ChatService : IChatService
{
    private readonly IClusterClient _clusterClient;
    private readonly IGrainObserverManager _grainObserverManager;
    private readonly ILogger<ChatService> _logger;
    private string _currentChat;

    public ChatService(
        IClusterClient clusterClient,
        IGrainObserverManager grainObserverManager,
        ILogger<ChatService> logger)
    {
        _clusterClient = clusterClient;
        _grainObserverManager = grainObserverManager;
        _logger = logger;
    }

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public async Task Join(string chat, Guid clientId)
    {
        _logger.LogDebug("Attempting to join {Chat}.", chat);
        await _grainObserverManager.Subscribe(this, chat);
        _currentChat = chat;
        _logger.LogDebug("Successfully joined {Chat}.", chat);
    }

    public Task ReceiveMessage(Guid clientId, string message)
    {
        _logger.LogDebug("Received message from {Client}", clientId);
        var eventArgs = new MessageReceivedEventArgs(clientId, message);
        MessageReceived?.Invoke(this, eventArgs);

        return Task.CompletedTask;
    }

    public async Task SendMessage(Guid clientId, string message)
    {
        _logger.LogDebug("Sending message to chat {Chat}", _currentChat);
        var grain = _clusterClient.GetGrain<IChatGrain>(_currentChat);
        await grain.SendMessage(clientId, message);
        _logger.LogDebug("Sent message to chat {Chat}", _currentChat);
    }
}

public class MessageReceivedEventArgs : EventArgs
{
    public MessageReceivedEventArgs(
        Guid clientId,
        string message)
    {
        Clientid = clientId;
        Message = message;
    }

    public Guid Clientid { get; init; }

    public string Message { get; init; }
}
