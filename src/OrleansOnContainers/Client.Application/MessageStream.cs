using Shared.Messages;
using System.Threading.Channels;

namespace Client.Application;

internal class MessageStream : IMessageStream
{
    private readonly Channel<IMessage> _channel = Channel.CreateUnbounded<IMessage>();

    public ChannelWriter<IMessage> GetWriter() => _channel.Writer;

    public IAsyncEnumerable<IMessage> ReadMessages(CancellationToken cancellationToken) => _channel.Reader.ReadAllAsync(cancellationToken);
}
