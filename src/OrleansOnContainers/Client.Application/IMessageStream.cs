using Shared.Messages;
using System.Threading.Channels;

namespace Client.Application;

public interface IMessageStream
{
    ChannelWriter<IMessage> GetWriter();

    IAsyncEnumerable<IMessage> ReadMessages(CancellationToken cancellationToken);
}
