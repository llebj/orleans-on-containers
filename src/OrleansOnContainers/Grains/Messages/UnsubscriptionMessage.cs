using GrainInterfaces;

namespace Grains.Messages;

[Alias("unsubscription-message")]
[GenerateSerializer]
[Immutable]
public record UnsubscriptionMessage(string Chat, string Sender, DateTimeOffset SentAt) : IMessage
{
    public MessageCategory Category => MessageCategory.System;

    public string Message => $"{Sender} has unsubscribed.";
}
