using GrainInterfaces;
using Grains.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Utilities;
using Shared.Messages;

namespace Grains;

public class OldChatGrain : Grain, IOldChatGrain
{
    private readonly ILogger<ChatGrain> _logger;
    private readonly ObserverManager<IChatObserver> _subscriptionManager;

    public OldChatGrain(
        ILogger<ChatGrain> logger,
        IOptions<ChatGrainOptions> options)
    {
        _logger = logger;
        _subscriptionManager = new ObserverManager<IChatObserver>(TimeSpan.FromSeconds(options.Value.ObserverTimeout), logger);
    }

    public Task SendMessage(Guid clientId, string message)
    {
        _logger.LogDebug("{ClientId} sent message to {PrimaryKey}.", clientId, this.GetPrimaryKeyString());

        var chatMessage = new ChatMessage(this.GetPrimaryKeyString(), clientId.ToString(), message);
        _subscriptionManager.Notify(o => o.ReceiveMessage(chatMessage));

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
