using Client.Options;
using Microsoft.Extensions.Options;

namespace Client.Services;

public class ChatService : IChatService
{
    private readonly ClientOptions _options;

    public ChatService(
        IOptions<ClientOptions> options)
    {
        _options = options.Value;
    }

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public Task ReceiveMessage(Guid clientId, string message)
    {
        throw new NotImplementedException();
    }

    public Task SendMessage(Guid clientId, string message)
    {
        throw new NotImplementedException();
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
