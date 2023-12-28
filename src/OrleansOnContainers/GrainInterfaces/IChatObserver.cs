using Orleans.Concurrency;
using Shared;

namespace GrainInterfaces;

public interface IChatObserver : IGrainObserver
{
    [OneWay]
    Task ReceiveMessage(ChatMessage message);
}
