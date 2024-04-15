using Client.Application.Contracts;
using GrainInterfaces;

namespace Client.Application;

internal class MessageClient : IMessageClient
{
    private readonly IGrainFactory _grainFactory;

    public MessageClient(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public async Task<Result> SendMessage(string chat, Guid clientId, string message)
    {
        var grainReference = _grainFactory.GetGrain<IChatGrain>(chat);
        await grainReference.SendMessage(clientId, message);

        return Result.Success();
    }
}
