using Client.Application.Contracts;
using Client.Application.Options;
using GrainInterfaces;
using Microsoft.Extensions.Options;
using Shared.Messages;

namespace Client.Application;

internal class ChatAccessClient : IChatAccessClient
{
    private readonly IGrainFactory _grainFactory;
    private readonly IMessageStreamWriterAllocator _messageStreamWriterAllocator;
    private readonly ResubscriberOptions _resubscriberOptions;
    private readonly TimeProvider _timeProvider;
    private ChatObserver? _observer;
    private Resubscriber? _resubscriber;

    public ChatAccessClient(
        IGrainFactory grainFactory,
        IMessageStreamWriterAllocator messageStreamWriterAllocator,
        IOptions<ResubscriberOptions> resubscriberOptions,
        TimeProvider timeProvider)
    {
        _grainFactory = grainFactory;
        _messageStreamWriterAllocator = messageStreamWriterAllocator;
        _resubscriberOptions = resubscriberOptions.Value;
        _timeProvider = timeProvider;
    }

    public async Task<Result> JoinChat(string chat, Guid clientId, string screenName)
    {
        var grainReference = _grainFactory.GetGrain<IChatGrain>(chat);
        var screenNameIsAvailable = await grainReference.ScreenNameIsAvailable(screenName);

        if (!screenNameIsAvailable)
        {
            return Result.Failure($"The screen name '{screenName}' is not available. Please select another one.");
        }

        var (Writer, ReleaseKey) = _messageStreamWriterAllocator.GetWriter();
        _observer = new ChatObserver(Writer);
        var observerReference = _grainFactory.CreateObjectReference<IChatObserver>(_observer);
        await grainReference.Subscribe(clientId, screenName, observerReference);

        _resubscriber = new Resubscriber(_resubscriberOptions.RefreshTimePeriod, _timeProvider);
        _resubscriber.Start(grainReference, clientId, observerReference);

        return Result.Success();
    }
     
    public async Task<Result> LeaveChat(string chat, Guid clientId)
    {
        var grainReference = _grainFactory.GetGrain<IChatGrain>(chat);
        await grainReference.Unsubscribe(clientId);
        _observer = null;

        if (_resubscriber is not null)
        {
            await _resubscriber.Stop();
            _resubscriber = null;
        }

        return Result.Success();
    }
}

internal class Resubscriber(
    TimeSpan period,
    TimeProvider timeProvider)
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly PeriodicTimer _timer = new(period, timeProvider);
    private Task? _task;

    public void Start(IChatGrain grain, Guid clientId, IChatObserver observerReference)
    {
        if (_task is not null)
        {
            return;
        }

        _task = Resubscribe(grain, clientId, observerReference);
    }

    public async Task Stop()
    {
        if (_task is null)
        {
            return;
        }

        _cancellationTokenSource.Cancel();
        await _task;
        _cancellationTokenSource.Dispose();
        _timer.Dispose();
    }

    private async Task Resubscribe(IChatGrain grain, Guid clientId, IChatObserver observerReference)
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(_cancellationTokenSource.Token))
            {
                await grain.Resubscribe(clientId, observerReference);
            }
        }
        catch (OperationCanceledException) { }
    }
}

internal class ChatObserver(MessageStreamWriter writer) : IChatObserver
{
    private readonly MessageStreamWriter _writer = writer;

    public async Task ReceiveMessage(IMessage message) => await _writer.WriteMessage(message, default);
}
