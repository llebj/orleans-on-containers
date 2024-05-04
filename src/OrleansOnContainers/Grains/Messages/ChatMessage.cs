using GrainInterfaces;

namespace Grains.Messages;

[Alias("chat-message")]
[GenerateSerializer]
[Immutable]
public record ChatMessage(string Chat, string Sender, string Message, DateTimeOffset SentAt) : IMessage
{
    public MessageCategory Category => MessageCategory.User;
}
