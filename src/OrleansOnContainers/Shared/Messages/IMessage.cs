namespace Shared.Messages;

public interface IMessage
{
    string Chat { get; }

    string ClientId { get; }

    string Message { get; }
}
