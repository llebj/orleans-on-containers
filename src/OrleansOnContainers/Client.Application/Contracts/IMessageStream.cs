using Shared.Messages;
using System.Threading.Channels;

namespace Client.Application.Contracts;

public interface IMessageStream
{
    ChannelWriter<IMessage> GetWriter();

    IAsyncEnumerable<IMessage> ReadMessages();
}
