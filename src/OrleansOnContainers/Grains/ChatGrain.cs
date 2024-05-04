using GrainInterfaces;
using Grains.Messages;
using Grains.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Grains;

public class ChatGrain : Grain, IChatGrain
{
    private readonly SubscriberManager _subscriberManager;
    private readonly ILogger<ChatGrain> _logger;
    private readonly TimeProvider _timeProvider;

    public ChatGrain(
        ILogger<ChatGrain> logger,
        IOptions<ChatGrainOptions> options,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _subscriberManager = new(logger, TimeSpan.FromSeconds(options.Value.ObserverTimeout), timeProvider);
        _timeProvider = timeProvider;
    }

    public Task Resubscribe(Guid clientId, IChatObserver observer)
    {
        if (!_subscriberManager.ClientIsSubscribed(clientId))
        {
            _logger.LogInformation("Client '{ClientId}' attempted to resubscribe without being subscribed.", clientId);
            throw new InvalidOperationException(
                $"Client '{clientId}' attempted to resubscribe to '{this.GetPrimaryKeyString()}' without an active subscription.");
        }

        _subscriberManager.UpdateObserver(clientId, observer);
        _logger.LogInformation("Client '{ClientId}' resubscribed.", clientId);

        return Task.CompletedTask;
    }

    public Task<bool> ScreenNameIsAvailable(string screenName)
    {
        if (string.IsNullOrWhiteSpace(screenName))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_subscriberManager.ScreenNameIsAvailable(screenName));
    }

    public async Task SendMessage(Guid clientId, string message)
    {
        var grainId = this.GetPrimaryKeyString();

        if (!_subscriberManager.ClientIsSubscribed(clientId))
        {
            _logger.LogInformation("Client '{ClientId}' attempted to send a message without being subscribed.", clientId);
            throw new InvalidOperationException(
                $"Client '{clientId}' attempted to send a message to the chat '{grainId}' without an active subscription.");
        }

        _logger.LogInformation("Client '{ClientId}' sent a message.", clientId);
        var chatMessage = new ChatMessage(grainId, _subscriberManager.GetClientScreenName(clientId), message, GetCurrentTime());
        await _subscriberManager.NotifyObservers(chatMessage);
    }

    public async Task Subscribe(Guid clientId, string screenName, IChatObserver observer)
    {
        if (_subscriberManager.ClientIsSubscribed(clientId))
        {
            _logger.LogInformation("Client '{ClientId}' attempted to subscribe when they are already subscribed.", clientId);
            throw new InvalidOperationException(
                $"Client '{clientId}' attempted to subscribe to '{this.GetPrimaryKeyString()}' when it is already subscribed.");
        }

        await ThrowIfScreenNameNotAvailable(screenName);

        _logger.LogInformation("Client '{ClientId}' subscribed as '{ScreenName}'.", clientId, screenName);
        _subscriberManager.AddSubscriber(clientId, screenName, observer);
        var message = new SubscriptionMessage(this.GetPrimaryKeyString(), screenName, GetCurrentTime());
        await _subscriberManager.NotifyObservers(message, observerId => observerId != clientId);
    }

    public async Task Unsubscribe(Guid clientId)
    {
        if (!_subscriberManager.ClientIsSubscribed(clientId))
        {
            _logger.LogInformation("Client '{ClientId}' attempted to unsubscribe without being subscribed.", clientId);
            throw new InvalidOperationException(
                $"Client '{clientId}' attempted to unsubscribe from '{this.GetPrimaryKeyString()} without an active subscription.'");
        }

        _logger.LogInformation("Client '{ClientId}' unsubscribed.", clientId);
        var subscriberState = _subscriberManager.RemoveSubscriber(clientId);
        var message = new UnsubscriptionMessage(this.GetPrimaryKeyString(), subscriberState.ScreenName, GetCurrentTime());
        await _subscriberManager.NotifyObservers(message);
    }

    private DateTimeOffset GetCurrentTime() => _timeProvider.GetUtcNow();

    private async Task ThrowIfScreenNameNotAvailable(string screenName)
    {
        var screenNameIsAvailable = await ScreenNameIsAvailable(screenName);

        if (!screenNameIsAvailable)
        {
            throw new ArgumentException($"The screen name '{screenName}' is not available in the chat '{this.GetPrimaryKeyString()}'.");
        }
    }
}

internal class SubscriberManager
{
    private readonly ILogger _logger;
    private readonly TimeSpan _observerTimeout;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<Guid, SubscriberState> _subscribers = [];

    public SubscriberManager(
        ILogger logger,
        TimeSpan observerTimeout,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _observerTimeout = observerTimeout;
        _timeProvider = timeProvider;
    }

    public int Count => _subscribers.Count;

    public void AddSubscriber(Guid clientId, string screenName, IChatObserver observer)
    {
        var observerState = new SubscriberState(screenName, GetCurrentTime(), observer);
        _subscribers.Add(clientId, observerState);
        _logger.LogDebug("Added client '{ClientId}'. Now managing {SubscriberCount} subscribers.", clientId, Count);
    }

    public bool ClientIsSubscribed(Guid clientId) => _subscribers.ContainsKey(clientId);

    public string GetClientScreenName(Guid clientId) => _subscribers[clientId].ScreenName;

    public async Task NotifyObservers(IMessage message, Func<Guid, bool>? predicate = null)
    {
        _logger.LogDebug("Notifying observers of message.");
        var tasks = new List<Task>();
        var taskClients = new Dictionary<int, Guid>();
        var failedClients = new HashSet<Guid>();

        foreach (var (Id, Subscriber) in _subscribers)
        {
            if ((GetCurrentTime() - Subscriber.LastSeen) > _observerTimeout)
            {
                _logger.LogDebug("Marking client '{ClientId}' as failed due to time out.", Id);
                failedClients.Add(Id);

                continue;
            }

            if (predicate is not null && !predicate(Id))
            {
                _logger.LogDebug("Excluding client '{ClientId}' from notification.", Id);
                continue;
            }

            var task = Subscriber.Observer.ReceiveMessage(message);
            tasks.Add(task);
            taskClients.Add(task.Id, Id);
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            failedClients = ProcessFailedTasks(failedClients, tasks, taskClients);
        }

        foreach (var client in failedClients)
        {
            RemoveSubscriber(client);
        }
    }

    public SubscriberState RemoveSubscriber(Guid clientId)
    {
        _subscribers.Remove(clientId, out var state);
        _logger.LogDebug("Removed client '{ClientId}'. Now managing {SubscriberCount} subscribers.", clientId, Count);

        return state!;
    }

    public bool ScreenNameIsAvailable(string screenName) => !_subscribers.Any(subscriber => subscriber.Value.ScreenName == screenName);

    public void UpdateObserver(Guid clientId, IChatObserver observer) => _subscribers[clientId].UpdateObserver(GetCurrentTime(), observer);

    private DateTimeOffset GetCurrentTime() => _timeProvider.GetUtcNow();

    private static HashSet<Guid> ProcessFailedTasks(HashSet<Guid> failedClients, IEnumerable<Task> tasks, Dictionary<int, Guid> taskClients)
    {
        foreach (var task in tasks)
        {
            if (task.IsFaulted)
            {
                failedClients.Add(taskClients[task.Id]);
            }
        }

        return failedClients;
    }
}

internal class SubscriberState(string ScreenName, DateTimeOffset LastSeen, IChatObserver Observer)
{
    public DateTimeOffset LastSeen { get; private set; } = LastSeen;

    public IChatObserver Observer { get; private set; } = Observer;

    public string ScreenName { get; private set; } = ScreenName;

    public void UpdateObserver(DateTimeOffset lastSeen, IChatObserver observer)
    {
        LastSeen = lastSeen;
        Observer = observer;
    }
}
