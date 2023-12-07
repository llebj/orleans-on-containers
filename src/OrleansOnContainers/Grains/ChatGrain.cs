using GrainInterfaces;
using Microsoft.Extensions.Logging;
using Orleans.Utilities;

namespace Grains;

public class ChatGrain : Grain, IChatGrain
{
    private readonly ILogger<ChatGrain> _logger;
    private readonly ObserverManager<IChatObserver> _subscriptionManager;

    public ChatGrain(
        ILogger<ChatGrain> logger)
    {
        _logger = logger;
        _subscriptionManager = new ObserverManager<IChatObserver>(TimeSpan.FromMinutes(5), logger);
    }

    public Task SendMessage(Guid clientId, string message)
    {
        _logger.LogDebug("Sending message from {ClientId}.", clientId);
        _subscriptionManager.Notify(o => o.ReceiveMessage(clientId, message));

        return Task.CompletedTask;
    }

    public Task Subscribe(IChatObserver observer)
    {
        _subscriptionManager.Subscribe(observer, observer);

        return Task.CompletedTask;
    }

    public Task Unsubscribe(IChatObserver observer)
    {
        _subscriptionManager.Unsubscribe(observer);

        return Task.CompletedTask;
    }
}
