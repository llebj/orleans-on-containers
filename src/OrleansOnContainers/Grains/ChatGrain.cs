using GrainInterfaces;
using Grains.Options;
using Microsoft.Extensions.Options;
using Shared.Messages;
using System.Collections;

namespace Grains;

public class ChatGrain : Grain, IChatGrain
{
    private readonly SubscriberManager _subscriberManager = new();
    private readonly TimeSpan _observerTimeout;
    private readonly TimeProvider _timeProvider;

    public ChatGrain(
        IOptions<ChatGrainOptions> options,
        TimeProvider timeProvider)
    {
        _observerTimeout = TimeSpan.FromSeconds(options.Value.ObserverTimeout);
        _timeProvider = timeProvider;
    }

    public Task Resubscribe(Guid clientId, IChatObserver observer)
    {
        if (!_subscriberManager.ClientIsSubscribed(clientId))
        {
            throw new InvalidOperationException(
                $"The client '{clientId}' attempted to resubscribe to '{this.GetPrimaryKeyString()}' without an active subscription.");
        }

        _subscriberManager.UpdateObserver(clientId, GetCurrentTime(), observer);

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
            throw new InvalidOperationException(
                $"The client '{clientId}' attempted to send a message to the chat '{grainId}' without an active subscription.");
        }

        var chatMessage = new ChatMessage(grainId, _subscriberManager.GetClientScreenName(clientId), message);
        await NotifyObservers(chatMessage);
    }

    public async Task Subscribe(Guid clientId, string screenName, IChatObserver observer)
    {
        if (_subscriberManager.ClientIsSubscribed(clientId))
        {
            throw new InvalidOperationException(
                $"The client '{clientId}' attempted to subscribe to '{this.GetPrimaryKeyString()}' when it is already subscribed.");
        }

        await ThrowIfScreenNameNotAvailable(screenName);
        _subscriberManager.AddSubscriber(clientId, screenName, GetCurrentTime(), observer);

        var message = new SubscriptionMessage(this.GetPrimaryKeyString(), screenName);
        await NotifyObservers(message, observerId => observerId != clientId);
    }

    public async Task Unsubscribe(Guid clientId)
    {
        if (!_subscriberManager.ClientIsSubscribed(clientId))
        {
            throw new InvalidOperationException(
                $"The client '{clientId}' attempted to unsubscribe from '{this.GetPrimaryKeyString()} without an active subscription.'");
        }

        var subscriberState = _subscriberManager.RemoveSubscriber(clientId);
        var message = new UnsubscriptionMessage(this.GetPrimaryKeyString(), subscriberState.ScreenName);
        await NotifyObservers(message);
    }

    private DateTimeOffset GetCurrentTime() => _timeProvider.GetUtcNow();

    private async Task NotifyObservers(IMessage message, Func<Guid, bool>? predicate = null)
    {
        var currentTime = GetCurrentTime();
        var tasks = new List<Task>();
        var taskClients = new Dictionary<int, Guid>();
        var failedClients = new HashSet<Guid>();

        foreach (var (Id, Subscriber) in _subscriberManager)
        {
            if ((currentTime - Subscriber.LastSeen) > _observerTimeout)
            {
                failedClients.Add(Id);

                continue;
            }

            if (predicate is not null && !predicate(Id))
            {
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
            _subscriberManager.RemoveSubscriber(client);
        }
    }

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

    private async Task ThrowIfScreenNameNotAvailable(string screenName)
    {
        var screenNameIsAvailable = await ScreenNameIsAvailable(screenName);

        if (!screenNameIsAvailable)
        {
            throw new ArgumentException($"The screen name '{screenName}' is not available in the chat '{this.GetPrimaryKeyString()}'.");
        }
    }
}

internal class SubscriberManager : IEnumerable<KeyValuePair<Guid, SubscriberState>>
{
    private readonly Dictionary<Guid, SubscriberState> _subscribers = [];

    public int Count => _subscribers.Count;

    public void AddSubscriber(Guid clientId, string screenName, DateTimeOffset currentTime, IChatObserver observer)
    {
        var observerState = new SubscriberState(screenName, currentTime, observer);
        _subscribers.Add(clientId, observerState);
    }

    public bool ClientIsSubscribed(Guid clientId) => _subscribers.ContainsKey(clientId);

    public string GetClientScreenName(Guid clientId) => _subscribers[clientId].ScreenName;

    public IEnumerator<KeyValuePair<Guid, SubscriberState>> GetEnumerator() => _subscribers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _subscribers.GetEnumerator();

    public SubscriberState RemoveSubscriber(Guid clientId)
    {
        _subscribers.Remove(clientId, out var state);

        return state!;
    }

    public bool ScreenNameIsAvailable(string screenName) => !_subscribers.Any(subscriber => subscriber.Value.ScreenName == screenName);

    public void UpdateObserver(Guid clientId, DateTimeOffset currentTime, IChatObserver observer) => _subscribers[clientId].UpdateObserver(currentTime, observer);
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
