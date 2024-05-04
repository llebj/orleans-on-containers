namespace GrainInterfaces;

public interface IMessage
{
    MessageCategory Category { get; }

    string Chat { get; }

    string Message { get; }

    string Sender { get; }

    DateTimeOffset SentAt { get; }
}

public enum MessageCategory
{
    User,
    System
}