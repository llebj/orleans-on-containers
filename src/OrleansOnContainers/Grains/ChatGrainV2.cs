using GrainInterfaces;

namespace Grains;

public class ChatGrainV2 : Grain, IChatGrain
{
    public Task SendMessage(Guid clientId, string message)
    {
        throw new NotImplementedException();
    }

    public Task Subscribe(IChatObserver observer)
    {
        throw new NotImplementedException();
    }

    public Task Unsubscribe(IChatObserver observer)
    {
        throw new NotImplementedException();
    }
}
