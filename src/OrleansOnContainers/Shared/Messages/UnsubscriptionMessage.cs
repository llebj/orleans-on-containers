using Shared.Helpers;

namespace Shared.Messages;

public record UnsubscriptionMessage(string Chat, string ScreenName) : IMessage
{
    public MessageCategory Category => MessageCategory.System;

    public string Chat { get; } = Chat;

    public string ScreenName { get; } = ScreenName;

    public string Message => $"{ScreenName} has unsubscribed.";

    public override string ToString() => MessageBuilder.Build(Message, Chat, ScreenName);
}
