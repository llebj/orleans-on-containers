using GrainInterfaces;

namespace Grains.Messages;

[Alias("subscription-message")]
[GenerateSerializer]
[Immutable]
public record SubscriptionMessage(string Chat, string Sender, DateTimeOffset SentAt) : IMessage
{
    public MessageCategory Category => MessageCategory.System;

    public string Message => $"{Sender} has subscribed.";
}
