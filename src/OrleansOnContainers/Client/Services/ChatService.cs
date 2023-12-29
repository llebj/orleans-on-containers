using GrainInterfaces;
using Microsoft.Extensions.Logging;

namespace Client.Services;

public class ChatService : IChatService
{
    private readonly IChatObserver _chatObserver;
    private readonly IClusterClient _clusterClient;
    private readonly IGrainObserverManager _grainObserverManager;
    private readonly ILogger<ChatService> _logger;

    // TODO: These fields want to be replaced by properties on the IGrainObserverManager.
    private string? _currentChat;
    private bool _hasValidSubscription => !string.IsNullOrEmpty(_currentChat);

    public ChatService(
        IChatObserver chatObserver,
        IClusterClient clusterClient,
        IGrainObserverManager grainObserverManager,
        ILogger<ChatService> logger)
    {
        _chatObserver = chatObserver;
        _clusterClient = clusterClient;
        _grainObserverManager = grainObserverManager;
        _logger = logger;
    }

    public async Task<Result> Join(string chat, Guid clientId)
    {
        _logger.LogDebug("Attempting to join {Chat}.", chat);

        try
        {
            var subscribeResult = await _grainObserverManager.Subscribe(_chatObserver, chat);

            if (!subscribeResult.IsSuccess)
            {
                _logger.LogWarning("Failed to join {Chat}: {Message}", chat, subscribeResult.Message);

                return Result.Failure($"Failed to join {chat}: {subscribeResult.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join {Chat}.", chat);

            return Result.Failure(ex.Message);
        }

        _currentChat = chat;
        _logger.LogInformation("Successfully joined {Chat}.", chat);

        return Result.Success();
    }

    public async Task<Result> SendMessage(Guid clientId, string message)
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
