using Client.Application.Contracts;
using Shared.Messages;
using System.Threading.Channels;

namespace Client.Application;

public class MessageStream : IMessageStream
{
    private readonly Channel<IMessage> _channel = Channel.CreateUnbounded<IMessage>();

    public ChannelWriter<IMessage> GetWriter() => _channel.Writer;

    public IAsyncEnumerable<IMessage> ReadMessages() => _channel.Reader.ReadAllAsync();
}
