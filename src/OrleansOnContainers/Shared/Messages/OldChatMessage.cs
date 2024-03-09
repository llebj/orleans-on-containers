using Shared.Helpers;

namespace Shared.Messages;
public record OldChatMessage(string Chat, string ScreenName, string Message)
{
    public string Chat { get; } = Chat;

    public string ScreenName { get; } = ScreenName;

    public string Message { get; } = Message;

    public override string ToString() => MessageBuilder.Build(Message, Chat, ScreenName);
}
