using GrainInterfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Client;

public class ChatHostedService : BackgroundService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<ChatHostedService> _logger;

    private Guid _clientId = Guid.NewGuid();
    private int _grainId = 0;

    public ChatHostedService(
        IClusterClient clusterClient,
        ILogger<ChatHostedService> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Executing {WorkerName} {ClientId}", nameof(ChatHostedService), _clientId);
        var random = new Random();

        while (!stoppingToken.IsCancellationRequested)
        {
            var chatGrain = _clusterClient.GetGrain<IChatGrain>(_grainId);
            var message = $"It is {DateTime.Now.TimeOfDay}";
            _logger.LogDebug("Sending \"{Message}\" to {GrainId}", message, _grainId);
            await chatGrain.Message(_clientId, message);

            await Task.Delay(random.Next(5000, 60000), stoppingToken);
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {WorkerName} {ClientId}", nameof(ChatHostedService), _clientId);
        var chatGrain = _clusterClient.GetGrain<IChatGrain>(0);

        _logger.LogDebug("Joining chat {GrainId}", _grainId);
        await chatGrain.Join(_clientId);
        _logger.LogDebug("Joined chat {GrainId}", _grainId);

        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping {WorkerName} {Client}", nameof(ChatHostedService), _clientId);
        var chatGrain = _clusterClient.GetGrain<IChatGrain>(0);

        _logger.LogDebug("Leaving chat {GrainId}", _grainId);
        await chatGrain.Leave(_clientId);
        _logger.LogDebug("Left chat {GrainId}", _grainId);

        await base.StopAsync(cancellationToken);
    }
}
