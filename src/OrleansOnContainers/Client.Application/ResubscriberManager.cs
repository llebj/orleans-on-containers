using Client.Application.Options;
using GrainInterfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Client.Application;

internal interface IResubscriberManager
{
    bool IsManagingResubscriber { get; }

    Task StartResubscribing(IChatGrain grain, Guid clientId, IChatObserver observerReference);

    Task StopResubscribing();
}

internal class ResubscriberManager : IResubscriberManager
{
    private readonly ILogger<ResubscriberManager> _logger;
    private readonly ResubscriberOptions _options;
    private readonly TimeProvider _timeProvider;
    private Resubscriber? _resubscriber;

    public ResubscriberManager(
        ILogger<ResubscriberManager> logger,
        IOptions<ResubscriberOptions> options,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public bool IsManagingResubscriber => _resubscriber is not null;

    public Task StartResubscribing(IChatGrain grain, Guid clientId, IChatObserver observerReference)
    {
        if (IsManagingResubscriber)
        {
            _logger.LogWarning("Request made to begin resubscription when the process is already running.");
            throw new InvalidOperationException("Unable to start resubscription. Resubscription is already running.");
        }

        var resubscriber = new Resubscriber(_options.RefreshTimePeriod, _timeProvider);
        resubscriber.Start(grain, clientId, observerReference);

        return Task.CompletedTask;
    }

    public async Task StopResubscribing()
    {
        if (!IsManagingResubscriber)
        {
            return;
        }

        await _resubscriber!.Stop();
        _resubscriber = null;

        return;
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
