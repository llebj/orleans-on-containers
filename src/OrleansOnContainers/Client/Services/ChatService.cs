using GrainInterfaces;
using Microsoft.Extensions.Logging;

namespace Client.Services;

public class ChatService : IChatService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IClusterClient clusterClient,
        ILogger<ChatService> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public Task Join(string chat, Guid clientId)
    {
        throw new NotImplementedException();
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
        var chatId = 0;

        _logger.LogDebug("Sending message to chat {Chat}", chatId);
        var grain = _clusterClient.GetGrain<IChatGrain>(chatId);
        await grain.SendMessage(clientId, message);
        _logger.LogDebug("Sent message to chat {Chat}", chatId);
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
