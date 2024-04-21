using Client.Application.Contracts;
using Microsoft.Extensions.Logging;
using Shared.Messages;
using System.Threading.Channels;

namespace Client.Application;

internal class MessageStream(ILogger<MessageStream> logger) : IMessageStreamOutput, IMessageStreamInput
{
    private readonly ILogger<MessageStream> _logger = logger;
    private readonly ChannelAllocationManager _inputChannelAllocator = new();
    private readonly ChannelAllocationManager _outputChannelAllocator = new();
    private Channel<IMessage> _channel = GetChannel();

    public ChannelStatus InputStatus => _inputChannelAllocator.Status;

    public ChannelStatus OutputStatus => _outputChannelAllocator.Status;

    public (MessageStreamReader Reader, Guid ReleaseKey) GetReader()
    {
        _logger.LogDebug("New channel reader requested.");
        var allocationSucceeded = _outputChannelAllocator.TryAllocateChannel();

        if (!allocationSucceeded)
        {
            _logger.LogWarning("Failed to allocate channel reader. The reader has already been allocated.");
            throw new InvalidOperationException("The message stream reader has already been allocated.");
        }

        var (ReleaseKey, Token) = _outputChannelAllocator.ReleaseInformation;
        var reader = new MessageStreamReader(_channel.Reader, Token);
        _logger.LogInformation("New channel reader allocated with key '{ReleaseKey}'.", ReleaseKey);

        return (reader, ReleaseKey);
    }

    public (MessageStreamWriter Writer, Guid ReleaseKey) GetWriter()
    {
        _logger.LogDebug("New channel writer requested.");
        var allocationSucceeded = _inputChannelAllocator.TryAllocateChannel();

        if (!allocationSucceeded)
        {
            _logger.LogWarning("Failed to allocate channel writer. The writer has already been allocated.");
            throw new InvalidOperationException("The message stream writer has already been allocated.");
        }

        var (ReleaseKey, Token) = _inputChannelAllocator.ReleaseInformation;
        var writer = new MessageStreamWriter(_channel.Writer, Token);
        _logger.LogInformation("New channel writer allocated with key '{ReleaseKey}'.", ReleaseKey);

        return (writer, ReleaseKey);
    }

    public void ReleaseReader(Guid releaseKey)
    {
        _logger.LogDebug("Release of channel reader requested.");
        var releaseSucceeded = _outputChannelAllocator.TryReleaseChannel(releaseKey);

        if (!releaseSucceeded)
        {
            _logger.LogWarning("Failed to release channel reader with provided key '{ReleaseKey}''.", releaseKey);
            throw new InvalidOperationException("The provided release key does not match that of the allocated writer.");
        }

       _logger.LogInformation("Channel reader released with key '{ReleaseKey}'.", releaseKey);

        if (InputStatus == ChannelStatus.AwaitingCompletion)
        {
            ResetChannel();
        }
    }

    public void ReleaseWriter(Guid releaseKey)
    {
        _logger.LogDebug("Release of channel writer requested.");
        var releaseSucceeded = _inputChannelAllocator.TryReleaseChannel(releaseKey);

        if (!releaseSucceeded)
        {
            _logger.LogWarning("Failed to release channel writer with provided key '{ReleaseKey}'.", releaseKey);
            throw new InvalidOperationException("The provided release key does not match that of the allocated writer.");
        }

        _logger.LogInformation("Channel writer released with key '{ReleaseKey}'.", releaseKey);

        if (OutputStatus == ChannelStatus.Allocated)
        {
            _ = _channel.Writer.TryComplete();
        }
        else if (OutputStatus == ChannelStatus.AwaitingCompletion)
        {
            ResetChannel();
        }
    }

    private static Channel<IMessage> GetChannel() => Channel.CreateUnbounded<IMessage>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

    private void ResetChannel()
    {
        _channel = GetChannel();
        _ = _inputChannelAllocator.TryCompleteChannel();
        _ = _outputChannelAllocator.TryCompleteChannel();
    }
}

internal class ChannelAllocationManager
{
    private Allocation _allocation = Allocation.None;

    public ChannelStatus Status { get; private set; }

    public (Guid ReleaseKey, RevocationToken Token) ReleaseInformation => 
        _allocation != Allocation.None ?
        (_allocation.ReleaseKey, _allocation.RevocationTokenSource!.GetToken()) :
        (default, RevocationToken.None);

    public bool TryAllocateChannel()
    {
        if (Status != ChannelStatus.Open)
        {
            return false;
        }

        _allocation = new(Guid.NewGuid(), new RevocationTokenSource());
        Status = ChannelStatus.Allocated;

        return true;
    }

    public bool TryCompleteChannel()
    {
        if (Status != ChannelStatus.AwaitingCompletion)
        {
            return false;
        }

        Status = ChannelStatus.Open;

        return true;
    }

    public bool TryReleaseChannel(Guid releaseKey)
    {
        if (Status != ChannelStatus.Allocated || 
            _allocation.IsNone || 
            _allocation.ReleaseKey != releaseKey)
        {
            return false;
        }

        _allocation.RevocationTokenSource!.Revoke();
        _allocation = Allocation.None;
        Status = ChannelStatus.AwaitingCompletion;

        return true;
    }
}

internal readonly record struct Allocation
{
    public Allocation(Guid releaseKey, RevocationTokenSource revocationTokenSource)
    {
        ReleaseKey = releaseKey;
        RevocationTokenSource = revocationTokenSource;
    }

    public bool IsNone => this == None;

    public static Allocation None { get; } = default;

    public Guid ReleaseKey { get; }

    public RevocationTokenSource? RevocationTokenSource { get; }
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

    public void Revoke() => RevocationRequested = true;
}

public readonly struct RevocationToken(RevocationTokenSource? source)
{
    private readonly RevocationTokenSource? _source = source;

    public bool CanBeRevoked => _source is not null;

    public bool IsRevoked => _source is not null && _source.RevocationRequested;

    public static RevocationToken None => default;
}
