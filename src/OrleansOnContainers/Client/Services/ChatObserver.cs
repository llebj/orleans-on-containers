using GrainInterfaces;
using Microsoft.Extensions.Logging;

namespace Client.Services;

/// <summary>
/// A thin wrapper around IMessageStream that gets registered as a grain observer.
/// </summary>
/// <remarks>
/// Due to this class acting as a grain observer and being treated as a grain,
/// there are restrictions placed on this class (i.e., no properties). This means
/// that a separate class is required to surface the incoming messages as an observable (IMessageStream).
/// </remarks>
internal class ChatObserver : IChatObserver
{
    private readonly ILogger<ChatObserver> _logger;
    private readonly IMessageStream _incomingMessages;

    public ChatObserver(
        ILogger<ChatObserver> logger,
        IMessageStream incomingMessages)
    {
        _logger = logger;
        _incomingMessages = incomingMessages;
    }

    public async Task ReceiveMessage(Guid clientId, string message)
    {
        _logger.LogDebug("Received message from {Client}.", clientId);
        await _incomingMessages.Push(new ReceivedMessage(clientId, message));
    }
}
