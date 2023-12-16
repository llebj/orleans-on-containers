using GrainInterfaces;

namespace Client.Services;

internal interface IChatService : IChatObserver
{
    Task Join(string chat, Guid clientId);

    Task SendMessage(Guid clientId, string message);
}
