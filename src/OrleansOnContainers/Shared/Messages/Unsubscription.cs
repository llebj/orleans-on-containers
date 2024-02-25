using Shared.Helpers;

namespace Shared.Messages;

public record Unsubscription(string Chat, Guid ClientId) : IMessage
{
    private readonly Guid _clientId = ClientId;

    public string Chat { get; } = Chat;

    public Guid ClientId { get; } = default;

    public string Message => $"{_clientId} has unsubscribed.";

    public override string ToString() => MessageBuilder.Build(Message, SystemMessage.Id);
}
