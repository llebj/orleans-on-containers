using GrainInterfaces;
using Microsoft.Extensions.Logging;
using Orleans;

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
        _logger.LogInformation("Client {ClientId} joined chat {GrainId}", clientId, IdentityString);

        return Task.CompletedTask;
    }

    public Task Leave(Guid clientId)
    {
        _logger.LogInformation("Client {ClientId} left chat {GrainId}", clientId, IdentityString);

        return Task.CompletedTask;
    }

    public Task Message(string message)
    {
        _logger.LogInformation("Client {ClientId} says {Message}", message);

        return Task.CompletedTask;
    }
}
