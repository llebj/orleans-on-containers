using Client.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Client.Services;

public class BackgroundTimer<T> : IPeriodicTimer<T>
{
    private readonly ILogger<BackgroundTimer<T>> _logger;
    private readonly ObserverManagerOptions _options;
    private readonly TimeProvider _timerProvider;

    private CancellationTokenSource? _cancellationTokenSource;
    private PeriodicTimer? _timer;
    private T? _state;
    private Func<T, Task>? _timerDelegate;

    public BackgroundTimer(
        ILogger<BackgroundTimer<T>> logger,
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

    public Task Start(T state, Func<T, Task> timerDelegate)
    {
        _logger.LogDebug("Starting background timer.");

        if (IsStarted)
        {
            _logger.LogDebug("Background timer is already running.");
            Stop();
            _logger.LogDebug("Restarting background timer.");
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

    public Task Stop()
    {
        _logger.LogDebug("Stopping background timer.");
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _timer?.Dispose();

        _cancellationTokenSource = null;
        _timer = null;
        _state = default;
        _timerDelegate = null;

        return Task.CompletedTask;
    }

    private async Task Run(CancellationToken cancellationToken)
    {
        try
        {
            while (await _timer!.WaitForNextTickAsync(cancellationToken))
            {
                await _timerDelegate!(_state!);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute for timer delegate.");
        }
    }
}
