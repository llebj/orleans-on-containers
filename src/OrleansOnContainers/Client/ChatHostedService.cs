using GrainInterfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Client;

public class ChatHostedService : BackgroundService, IChat
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<ChatHostedService> _logger;
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly int _chatId = 0;

    private IChat? _reference;

    public ChatHostedService(
        IClusterClient clusterClient,
        ILogger<ChatHostedService> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Executing hosted service.");
        var chatGrain = _clusterClient.GetGrain<IChatGrain>(_chatId);
        _logger.LogDebug("Subscribing to {Chat}.", _chatId);
        await chatGrain.Subscribe(_reference!);
        var random = new Random();

        while (!stoppingToken.IsCancellationRequested)
        {
            var message = $"It is {DateTime.Now.TimeOfDay}";
            _logger.LogDebug("Sending message to {Chat}.", _chatId);
            await chatGrain.SendMessage(_clientId, message);

            await Task.Delay(random.Next(5000, 60000), stoppingToken);
        }

        _logger.LogDebug("Unsubscribing to {Chat}.", _chatId);
        await chatGrain.Unsubscribe(_reference!);
    }

    public Task ReceiveMessage(Guid clientId, string message)
    {
        _logger.LogDebug("{Client} says \"{Message}\".", clientId, message);

        return Task.CompletedTask;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _reference = _clusterClient.CreateObjectReference<IChat>(this);

        return base.StartAsync(cancellationToken);
    }
}
