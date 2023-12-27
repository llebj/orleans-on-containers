using System.Reactive.Subjects;

namespace Client.Services;

internal class MessageStream : IMessageStream, IDisposable
{
    private readonly Subject<ReceivedMessage> _receivedMessagesSubject = new();

    public IObservable<ReceivedMessage> Messages => _receivedMessagesSubject;

    public void Dispose()
    {
        _receivedMessagesSubject.Dispose();
    }

    public Task Push(ReceivedMessage message)
    {
        _receivedMessagesSubject?.OnNext(message);

        return Task.CompletedTask;
    }
}
