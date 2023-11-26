using GrainInterfaces;
using Microsoft.Extensions.Logging;
using Orleans.Utilities;

namespace Grains;

public class ChatGrain : Grain, IChatGrain
{
    private readonly ILogger<ChatGrain> _logger;
    private readonly ObserverManager<IChat> _subscriptionManager;

    public ChatGrain(
        ILogger<ChatGrain> logger)
    {
        _logger = logger;
        _subscriptionManager = new ObserverManager<IChat>(TimeSpan.FromMinutes(5), logger);
    }

    public Task SendMessage(Guid clientId, string message)
    {
        _logger.LogDebug("Sending message from {ClientId}.", clientId);
        _subscriptionManager.Notify(o => o.ReceiveMessage(clientId, message));

        return Task.CompletedTask;
    }

    public Task Subscribe(IChat observer)
    {
        _subscriptionManager.Subscribe(observer, observer);

        return Task.CompletedTask;
    }

    public Task Unsubscribe(IChat observer)
    {
        _subscriptionManager.Unsubscribe(observer);

        return Task.CompletedTask;
    }
}
