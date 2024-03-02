using Shared.Helpers;

namespace Shared.Messages;

public class Subscription(string Chat, string ClientId) : IMessage
{
    public MessageCategory Category => MessageCategory.System;

    public string Chat { get; } = Chat;

    public string ClientId { get; } = ClientId;

    public string Message => $"{ClientId} has subscribed.";

    public override string ToString() => MessageBuilder.Build(Message, Chat, ClientId);
}
