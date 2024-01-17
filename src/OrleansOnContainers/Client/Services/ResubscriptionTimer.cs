using Client.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Client.Services;

/// <summary>
/// Directly uses a PeriodicTimer to execute a client-provided async delagate.
/// </summary>
/// <remarks>
/// The call to run the PeriodiceTimer within the Register method is not awaited
/// as doing so would result in the method never returning. This makes it difficult
/// to test this class without resulting in a race condition. Using a BackgroundService
/// for the resubscription and providing an API for passing state and delagates into
/// the service could prove a nicer implementation.
/// </remarks>
/// <typeparam name="T"></typeparam>
public class ResubscriptionTimer<T> : IResubscriber<T>
{
    private readonly ILogger<ResubscriptionTimer<T>> _logger;
    private readonly ObserverManagerOptions _options;
    private readonly TimeProvider _timerProvider;

    private CancellationTokenSource? _cancellationTokenSource;
    private PeriodicTimer? _timer;

    public ResubscriptionTimer(
        ILogger<ResubscriptionTimer<T>> logger,
        IOptions<ObserverManagerOptions> options,
        TimeProvider timerProvider)
    {
        _logger = logger;
        _options = options.Value;
        _timerProvider = timerProvider;
    }

    private bool IsStarted => _timer is not null;

    public Task Clear()
    {
        _logger.LogDebug("Stopping resubscription timer.");
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        _timer?.Dispose();
        _timer = null;

        return Task.CompletedTask;
    }

    public Task Register(T state, Func<T, Task> timerDelegate)
    {
        _logger.LogDebug("Starting resubscription timer.");

        if (IsStarted)
        {
            _logger.LogDebug("Resubscription timer is already running.");
            Clear();
            _logger.LogDebug("Restarting resubscription timer.");
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_options.RefreshPeriod), 
            _timerProvider);

        _ = Run(state, timerDelegate, _cancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    private async Task Run(T state, Func<T, Task> timerDelegate, CancellationToken cancellationToken)
    {
        try
        {
            while (await _timer!.WaitForNextTickAsync(cancellationToken))
            {
                await timerDelegate(state);
                _logger.LogDebug("Executed timer delegate.");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute timer delegate.");
        }
    }
}
