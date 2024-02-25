using Shared.Helpers;

namespace Shared.Messages;

public record ChatMessage(string Chat, Guid ClientId, string Message) : IMessage
{
    public string Chat { get; } = Chat;

    public Guid ClientId { get; } = ClientId;

    public string Message { get; } = Message;

    public override string ToString() => MessageBuilder.Build(Message, Chat, ClientId);
}
