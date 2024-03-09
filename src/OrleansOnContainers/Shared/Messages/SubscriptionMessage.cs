using Shared.Helpers;

namespace Shared.Messages;

public class SubscriptionMessage(string Chat, Guid ClientId) : IMessage
{
    public MessageCategory Category => MessageCategory.System;

    public string Chat { get; } = Chat;

    public Guid ClientId { get; } = ClientId;

    public string Message => $"{ClientId} has subscribed.";

    public override string ToString() => MessageBuilder.Build(Message, Chat, ClientId);
}
