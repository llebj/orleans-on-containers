namespace Client.Services;

internal interface IMessageStream
{
    IObservable<ReceivedMessage> Messages { get; }

    Task Push(ReceivedMessage message);
}

internal class ReceivedMessage(Guid clientId, string message)
{
    public Guid ClientId { get; } = clientId;

    public string Message { get; } = message;
}