using Shared.Helpers;

namespace Shared.Messages;

public record UnsubscriptionMessage(string Chat, Guid ClientId) : IMessage
{
    public MessageCategory Category => MessageCategory.System;

    public string Chat { get; } = Chat;

    public Guid ClientId { get; } = ClientId;

    public string Message => $"{ClientId} has unsubscribed.";

    public override string ToString() => MessageBuilder.Build(Message, Chat, ClientId);
}
