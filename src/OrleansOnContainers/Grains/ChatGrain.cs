using GrainInterfaces;
using Microsoft.Extensions.Logging;

namespace Grains;

public class ChatGrain : Grain, IChatGrain
{
    private readonly ILogger<ChatGrain> _logger;

    public ChatGrain(
        ILogger<ChatGrain> logger)
    {
        _logger = logger;
    }

    public Task Join(Guid clientId)
    {
        _logger.LogInformation("{ClientId} is online", clientId);

        return Task.CompletedTask;
    }

    public Task Leave(Guid clientId)
    {
        _logger.LogInformation("{ClientId} is offline", clientId);

        return Task.CompletedTask;
    }

    public Task Message(Guid clientId, string message)
    {
        _logger.LogDebug("{ClientId} says {Message}", clientId, message);

        return Task.CompletedTask;
    }
}
