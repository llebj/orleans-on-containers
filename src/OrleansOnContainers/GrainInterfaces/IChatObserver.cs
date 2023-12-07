using Orleans.Concurrency;

namespace GrainInterfaces;

public interface IChatObserver : IGrainObserver
{
    [OneWay]
    Task ReceiveMessage(Guid clientId, string message);
}
