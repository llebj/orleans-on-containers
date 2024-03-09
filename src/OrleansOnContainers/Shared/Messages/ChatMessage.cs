using Shared.Helpers;

namespace Shared.Messages;

public record ChatMessage(string Chat, string ScreenName, string Message) : IMessage
{
    public MessageCategory Category => MessageCategory.User;

    public string Chat { get; } = Chat;

    public string ScreenName { get; } = ScreenName;

    public string Message { get; } = Message;

    public override string ToString() => MessageBuilder.Build(Message, Chat, ScreenName);
}
