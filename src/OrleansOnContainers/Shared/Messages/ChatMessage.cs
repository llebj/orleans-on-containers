using Shared.Helpers;

namespace Shared.Messages;

public record ChatMessage(string Chat, string ClientId, string Message) : IMessage
{
    public string Chat { get; } = Chat;

    public string ClientId { get; } = ClientId;

    public string Message { get; } = Message;

    public override string ToString() => MessageBuilder.Build(Message, Chat, ClientId);
}
