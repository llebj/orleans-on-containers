using GrainInterfaces;
using Microsoft.Extensions.Logging;

namespace Client.Services;

public class ChatService : IChatService
{
    private readonly IClusterClient _clusterClient;
    private readonly IGrainObserverManager _grainObserverManager;
    private readonly ILogger<ChatService> _logger;

    private string? _currentChat;
    private bool _hasValidSubscription => !string.IsNullOrEmpty(_currentChat);

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

    public async Task<Result> Join(string chat, Guid clientId)
    {
        _logger.LogDebug("Attempting to join {Chat}.", chat);

        try
        {
            await _grainObserverManager.Subscribe(this, chat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join {Chat}.", chat);

            return Result.Failure(ex.Message);
        }

        _currentChat = chat;
        _logger.LogDebug("Successfully joined {Chat}.", chat);

        return Result.Success();
    }

    public Task ReceiveMessage(Guid clientId, string message)
    {
        _logger.LogDebug("Received message from {Client}", clientId);
        var eventArgs = new MessageReceivedEventArgs(clientId, message);
        MessageReceived?.Invoke(this, eventArgs);

        return Task.CompletedTask;
    }

    public async Task<Result> SendMessage(Guid clientId, string message)
    {
        _logger.LogDebug("Sending message to chat {Chat}", _currentChat);

        if (!_hasValidSubscription)
        {
            _logger.LogDebug("Failed to send message: no active subscription.");

            return Result.Failure("The message failed to send as there is no active subscription.");
        }

        var grain = _clusterClient.GetGrain<IChatGrain>(_currentChat);

        try
        {
            await grain.SendMessage(clientId, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message.");

            return Result.Failure("The message failed to send due to a network related error.");
        }
        
        _logger.LogDebug("Sent message to chat {Chat}", _currentChat);

        return Result.Success();
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
