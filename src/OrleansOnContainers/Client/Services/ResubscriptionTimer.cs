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
    private T? _state;
    private Func<T, Task>? _timerDelegate;

    public ResubscriptionTimer(
        ILogger<ResubscriptionTimer<T>> logger,
        IOptions<ObserverManagerOptions> options,
        TimeProvider timerProvider)
    {
        _logger = logger;
        _options = options.Value;
        _timerProvider = timerProvider;
    }

    private bool IsStarted => 
        _cancellationTokenSource != null ||
        _timer != null ||
        _state != null ||
        _timerDelegate != null;

    public Task Clear()
    {
        _logger.LogDebug("Stopping resubscription timer.");
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _timer?.Dispose();

        _cancellationTokenSource = null;
        _timer = null;
        _state = default;
        _timerDelegate = null;

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

        _state = state;
        _timerDelegate = timerDelegate;
        _cancellationTokenSource = new CancellationTokenSource();
        _timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_options.RefreshPeriod), 
            _timerProvider);

        _ = Run(_cancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    private async Task Run(CancellationToken cancellationToken)
    {
        try
        {
            while (await _timer!.WaitForNextTickAsync(cancellationToken))
            {
                await _timerDelegate!(_state!);
                _logger.LogDebug("Executed timer delegate.");
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute for timer delegate.");
        }
    }
}
