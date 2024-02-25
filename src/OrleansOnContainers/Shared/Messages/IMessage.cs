namespace Shared.Messages;

public interface IMessage
{
    string Chat { get; }

    Guid ClientId { get; }

    string Message { get; }
}
