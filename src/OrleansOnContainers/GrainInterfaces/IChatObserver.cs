using Orleans.Concurrency;
using Shared.Messages;

namespace GrainInterfaces;

public interface IChatObserver : IGrainObserver
{
    Task ReceiveMessage(IMessage message);
}
