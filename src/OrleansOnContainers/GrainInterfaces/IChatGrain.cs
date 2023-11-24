namespace GrainInterfaces;

public interface IChatGrain : IGrainWithIntegerKey
{
    Task Join(Guid clientId);

    Task Leave(Guid clientId);

    Task Message(Guid clientId, string message);
}
