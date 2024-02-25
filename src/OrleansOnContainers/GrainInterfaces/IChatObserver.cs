using Orleans.Concurrency;
using Shared.Messages;

namespace GrainInterfaces;

public interface IChatObserver : IGrainObserver
{
    [OneWay]
    Task ReceiveMessage(IMessage message);
}
