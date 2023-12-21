using Client.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Client.Services;

internal class ChatHostedService : BackgroundService
{
    private readonly string _chatId = "test";

    private readonly StringBuilder _buffer = new();
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHostedService> _logger;
    private readonly ClientOptions _options;

    public ChatHostedService(
        IChatService chatService,
        ILogger<ChatHostedService> logger,
        IOptions<ClientOptions> options)
    {
        _chatService = chatService;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Executing hosted service.");
        await _chatService.Join(_chatId, _options.ClientId);
        Console.WriteLine($"Joined {_chatId}.");
        var stringBuilder = new StringBuilder();

        while (!stoppingToken.IsCancellationRequested)
        {
            var keyInfo = Console.ReadKey(true);

            if (keyInfo.Modifiers == ConsoleModifiers.Control &&
                keyInfo.Key == ConsoleKey.Q)
            {
                break;
            }
            else if (keyInfo.Key != ConsoleKey.Enter)
            {
                Console.Write(keyInfo.KeyChar);
                stringBuilder.Append(keyInfo.KeyChar);

                continue;
            }

            ClearCurrentConsoleLine(stringBuilder.Length);
            await _chatService.SendMessage(_options.ClientId, _buffer.ToString());
            stringBuilder.Clear();
        }

        Console.WriteLine($"Leaving {_chatId}.");
        _logger.LogInformation("Finished executing hosted service.");
    }

    private static void ClearCurrentConsoleLine(int width)
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', width));
        Console.SetCursorPosition(0, Console.CursorTop);
    }
}
