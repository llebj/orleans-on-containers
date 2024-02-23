using GrainInterfaces;

namespace Client;

/// <summary>
/// Encapsulates the information required to subscribe an observer to a grain.
/// </summary>
/// <param name="grainId">The id of the grain to subscribe to.</param>
/// <param name="objectReference">An object reference created by IGrainFactory.CreateObjectReference().</param>
public class GrainSubscription(string grainId, IChatObserver objectReference)
{
    public string GrainId { get; init; } = grainId;

    public IChatObserver ObjectReference { get; init; } = objectReference;
}
