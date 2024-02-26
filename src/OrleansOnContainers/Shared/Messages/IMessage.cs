namespace Shared.Messages;

public interface IMessage
{
    MessageCategory Category { get; }

    string Chat { get; }

    string ClientId { get; }

    string Message { get; }
}

public enum MessageCategory
{
    User,
    System
}