namespace Client.Services;

internal interface IIncomingMessages
{
    IObservable<ReceivedMessage> Messages { get; }

    Task ReceiveMessage(ReceivedMessage message);
}

internal class ReceivedMessage(Guid clientId, string message)
{
    public Guid ClientId { get; } = clientId;

    public string Message { get; } = message;
}