using GrainInterfaces;

namespace Client;

public class GrainSubscription
{
    public string? GrainId { get; protected set; }

    public IChatObserver? Reference { get; protected set; }
}
