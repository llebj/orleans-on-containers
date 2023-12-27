using GrainInterfaces;

namespace Client.Services;

/// <summary>
/// A thin wrapper around IIncomingMessages that gets registered as a grain observer.
/// </summary>
/// <remarks>
/// Due to this class acting as a grain observer and being treated as a grain,
/// there are restrictions placed on this class (i.e., no properties), hence IIncomingMessages.
/// </remarks>
internal class ChatObserver : IChatObserver
{
    private readonly IIncomingMessages _incomingMessages;

    public ChatObserver(
        IIncomingMessages incomingMessages)
    {
        _incomingMessages = incomingMessages;
    }

    public async Task ReceiveMessage(Guid clientId, string message)
    {
        await _incomingMessages.ReceiveMessage(new ReceivedMessage(clientId, message));
    }
}
