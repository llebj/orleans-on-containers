using GrainInterfaces;
using Microsoft.Extensions.Logging;
using Shared.Messages;

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

    public async Task ReceiveMessage(IMessage message)
    {
        _logger.LogDebug("Received message from {Client}.", message.ClientId);

        // When this method is called from a grain then the grain will wait for a completion
        // message back. I think this means that the grain will have to wait for every client
        // to finish writing the message before it returns, causing delays. This could be avoided by
        // having the client return to the grain after accepting the message rather than after finishing
        // all of its processing.
        // TODO: Some testing will need to be done on the order of method calls in order to verify
        // observer behaviour.
        await _incomingMessages.Push(new OldChatMessage(message.Chat, Guid.TryParse(message.ClientId, out var client) ? client : default, message.Message));
    }
}
