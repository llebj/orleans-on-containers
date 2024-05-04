using Client.Application.Contracts;
using GrainInterfaces;
using Microsoft.Extensions.Logging;

namespace Client.Application;

internal interface IObserverManager
{
    bool IsManagingObserver { get; }

    IChatObserver CreateObserver();

    void DestroyObserver();
}

internal class ObserverManager : IObserverManager
{
    private readonly ILogger<ObserverManager> _logger;
    private readonly IMessageStreamInput _messageStreamInput;
    private ChatObserver? _observer;
    private Guid _releaseKey;

    public ObserverManager(
        ILogger<ObserverManager> logger,
        IMessageStreamInput messageStreamInput)
    {
        _logger = logger;
        _messageStreamInput = messageStreamInput;
    }

    public bool IsManagingObserver => _observer is not null;

    public IChatObserver CreateObserver()
    {
        if (IsManagingObserver)
        {
            _logger.LogWarning("Request to create observer when an observer is already being managed.");
            throw new InvalidOperationException("Unable to create observer. An observer is already being managed.");
        }

        var (Writer, ReleaseKey) = _messageStreamInput.GetWriter();
        var observer = new ChatObserver(Writer);
        _observer = observer;
        _releaseKey = ReleaseKey;

        return observer;
    }

    public void DestroyObserver()
    {
        if (!IsManagingObserver)
        {
            return;
        }

        _messageStreamInput.ReleaseWriter(_releaseKey);
        _releaseKey = default;
        _observer = null;
    }
}

internal class ChatObserver(MessageStreamWriter writer) : IChatObserver
{
    private readonly MessageStreamWriter _writer = writer;

    public async Task ReceiveMessage(IMessage message) => await _writer.WriteMessage(message, default);
}
