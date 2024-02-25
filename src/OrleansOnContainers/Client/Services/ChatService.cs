using GrainInterfaces;
using Microsoft.Extensions.Logging;

namespace Client.Services;

public class ChatService : IChatService
{
    private readonly IClusterClient _clusterClient;
    private readonly ISubscriptionManager _grainObserverManager;
    private readonly ILogger<ChatService> _logger;

    // TODO: These fields want to be replaced by properties on the IGrainObserverManager.
    private string? _currentChat;
    private bool _hasValidSubscription => !string.IsNullOrEmpty(_currentChat);

    public ChatService(
        IClusterClient clusterClient,
        ISubscriptionManager grainObserverManager,
        ILogger<ChatService> logger)
    {
        _clusterClient = clusterClient;
        _grainObserverManager = grainObserverManager;
        _logger = logger;
    }

    public async Task<Result> Join(string chat, Guid clientId)
    {
        _logger.LogDebug("Attempting to join {Chat}.", chat);
        var subscribeResult = await _grainObserverManager.Subscribe(chat);

        if (!subscribeResult.IsSuccess)
        {
            _logger.LogWarning("Failed to join {Chat}: {Message}", chat, subscribeResult.Message);

            return Result.Failure($"Failed to join {chat}: {subscribeResult.Message}");
        }

        _currentChat = chat;
        _logger.LogInformation("Successfully joined {Chat}.", chat);

        return Result.Success();
    }

    public async Task<Result> SendMessage(string clientId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return Result.Success();
        }

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
