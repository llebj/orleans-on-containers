using Microsoft.Extensions.Logging;
using Shared;
using System.Reactive.Subjects;

namespace Client.Services;

/// <summary>
/// Receives push-based messages and surfaces them to subscibers via an IObservable, implemented
/// using a Rx.NET Subject.
/// </summary>
internal class MessageStream : IMessageStream, IDisposable
{
    private readonly Subject<ChatMessage> _receivedMessagesSubject = new();
    private readonly ILogger<MessageStream> _logger;

    public MessageStream(ILogger<MessageStream> logger)
    {
        _logger = logger;
    }

    public IObservable<ChatMessage> Messages => _receivedMessagesSubject;

    public void Dispose()
    {
        _receivedMessagesSubject.Dispose();
    }

    public Task Push(ChatMessage message)
    {
        _logger.LogDebug("Pushing message.");
        _receivedMessagesSubject?.OnNext(message);

        return Task.CompletedTask;
    }
}
