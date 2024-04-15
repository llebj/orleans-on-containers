using Client.Application.Contracts;
using Microsoft.Extensions.Logging;
using Shared.Messages;
using System.Threading.Channels;

namespace Client.Application;

internal class MessageStream(ILogger<MessageStream> logger) : IMessageStreamReaderAllocator, IMessageStreamWriterAllocator
{
    private readonly Channel<IMessage> _channel = Channel.CreateUnbounded<IMessage>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
    private readonly ILogger<MessageStream> _logger = logger;
    private Allocation _currentReaderAllocation = Allocation.None;
    private Allocation _currentWriterAllocation = Allocation.None;

    public bool ReaderIsAllocated => !_currentReaderAllocation.IsNone;

    public bool WriterIsAllocated => !_currentWriterAllocation.IsNone;

    public (MessageStreamReader Reader, Guid ReleaseKey) GetReader()
    {
        _logger.LogDebug("New channel reader requested.");

        if (ReaderIsAllocated)
        {
            _logger.LogWarning("Failed to allocate channel reader. The reader has already been allocated.");
            throw new InvalidOperationException("The message stream reader has already been allocated.");
        }

        var releaseKey = Guid.NewGuid();
        var revocationTokenSource = new RevocationTokenSource();
        _currentReaderAllocation = new(releaseKey, revocationTokenSource);
        _logger.LogInformation("New channel reader allocated with key '{ReleaseKey}'.", releaseKey);

        var reader = new MessageStreamReader(_channel.Reader, revocationTokenSource.GetToken());

        return (reader, releaseKey);
    }

    public (MessageStreamWriter Writer, Guid ReleaseKey) GetWriter()
    {
        _logger.LogDebug("New channel writer requested.");

        if (WriterIsAllocated)
        {
            _logger.LogWarning("Failed to allocate channel writer. The writer has already been allocated.");
            throw new InvalidOperationException("The message stream writer has already been allocated.");
        }

        var releaseKey = Guid.NewGuid();
        var revocationTokenSource = new RevocationTokenSource();
        _currentWriterAllocation = new(releaseKey, revocationTokenSource);
        _logger.LogInformation("New channel writer allocated with key '{ReleaseKey}'.", releaseKey);

        var writer = new MessageStreamWriter(_channel.Writer, revocationTokenSource.GetToken());

        return (writer, releaseKey);
    }

    public void ReleaseReader(Guid releaseKey)
    {
        if (_currentReaderAllocation.IsNone)
        {
            return;
        }

        _logger.LogDebug("Release of channel reader requested.");
        ReleaseAllocation(_currentReaderAllocation, releaseKey);
        _logger.LogInformation("Channel writer released with key '{ReleaseKey}'.", releaseKey);
        _currentReaderAllocation = Allocation.None;
    }

    public void ReleaseWriter(Guid releaseKey)
    {
        if (_currentWriterAllocation.IsNone)
        {
            return;
        }

        _logger.LogDebug("Release of channel writer requested.");
        ReleaseAllocation(_currentWriterAllocation, releaseKey);
        _logger.LogInformation("Channel writer released with key '{ReleaseKey}'.", releaseKey);
        _currentWriterAllocation = Allocation.None;
    }

    private void ReleaseAllocation(Allocation allocation, Guid releaseKey)
    {
        if (allocation.ReleaseKey != releaseKey)
        {
            _logger.LogWarning("Failed to release allocation: the provided key '{ReleaseKey}' does not match '{ReleaseKey}'.", releaseKey, allocation.ReleaseKey);
            throw new InvalidOperationException("The provided release key does not match that of the allocated writer.");
        }

        allocation.RevocationTokenSource!.RequestRevocation();
    }

    private readonly record struct Allocation
    {
        public Allocation()
        {

        }

        public Allocation(Guid releaseKey, RevocationTokenSource revocationTokenSource)
        {
            ReleaseKey = releaseKey;
            RevocationTokenSource = revocationTokenSource;
        }

        public bool IsNone => this == None;

        public static Allocation None { get; } = new();

        public Guid? ReleaseKey { get; }

        public RevocationTokenSource? RevocationTokenSource { get; }
    }
}

/// <summary>
/// Controls client access to the ChannelReader of a message stream.
/// </summary>
/// <param name="channelWriter">The ChannelReader instance that will be accessed.</param>
/// <param name="revocationToken">A RevocationToken used to revoke access to the ChannelReader.</param>
/// <exception cref="InvalidOperationException">Thrown when this MessageStreamReader's access to the ChannelReader has been revoked.</exception>
public class MessageStreamReader(ChannelReader<IMessage> channelReader, RevocationToken revocationToken)
{
    private readonly ChannelReader<IMessage> _channelReader = channelReader;
    private readonly RevocationToken _revocationToken = revocationToken;

    public IAsyncEnumerable<IMessage> ReadMessages()
    {
        if (_revocationToken.IsRevoked)
        {
            throw new InvalidOperationException("This message stream reader has been revoked.");
        }

        return _channelReader.ReadAllAsync();
    }
}

/// <summary>
/// Controls client access to the ChannelWriter of a message stream.
/// </summary>
/// <param name="channelWriter">The ChannelWriter instance that will be accessed.</param>
/// <param name="revocationToken">A RevocationToken used to revoke access to the ChannelWriter.</param>
/// <exception cref="InvalidOperationException">Thrown when this MessageStreamWriter's access to the ChannelWriter has been revoked.</exception>
public class MessageStreamWriter(ChannelWriter<IMessage> channelWriter, RevocationToken revocationToken)
{
    private readonly ChannelWriter<IMessage> _channelWriter = channelWriter;
    private readonly RevocationToken _revocationToken = revocationToken;

    public async Task WriteMessage(IMessage message, CancellationToken cancellationToken)
    {
        if (_revocationToken.IsRevoked)
        {
            throw new InvalidOperationException("This message stream writer has been revoked.");
        }

        await _channelWriter.WriteAsync(message, cancellationToken);
    }
}

public class RevocationTokenSource()
{
    public bool RevocationRequested { get; private set; }

    public RevocationToken GetToken() => new(this);

    public void RequestRevocation() => RevocationRequested = true;
}

public readonly struct RevocationToken(RevocationTokenSource source)
{
    private readonly RevocationTokenSource _source = source;

    public bool IsRevoked => _source.RevocationRequested;
}
