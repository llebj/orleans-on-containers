using GrainInterfaces;

namespace Client.Services;

internal interface IChatService : IChatObserver
{
    Task SendMessage(Guid clientId, string message);
}
