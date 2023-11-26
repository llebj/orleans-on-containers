using Orleans.Concurrency;

namespace GrainInterfaces;

public interface IChat : IGrainObserver
{
    [OneWay]
    Task ReceiveMessage(Guid clientId, string message);
}
