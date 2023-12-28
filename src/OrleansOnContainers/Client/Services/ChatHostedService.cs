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
    private readonly IMessageStream _messageStream;
    private readonly ClientOptions _options;

    public ChatHostedService(
        IChatService chatService,
        ILogger<ChatHostedService> logger,
        IMessageStream messageStream,
        IOptions<ClientOptions> options)
    {
        _chatService = chatService;
        _logger = logger;
        _messageStream = messageStream;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Executing hosted service.");
        await _chatService.Join(_chatId, _options.ClientId);
        Console.WriteLine($"Joined {_chatId}.");

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
                _buffer.Append(keyInfo.KeyChar);

                continue;
            }

            ClearCurrentConsoleLine(_buffer.Length);
            await _chatService.SendMessage(_options.ClientId, _buffer.ToString());
            _buffer.Clear();
        }

        Console.WriteLine($"Leaving {_chatId}.");
        _logger.LogInformation("Finished executing hosted service.");
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _ =_messageStream.Messages.Subscribe(message =>
        {
            ClearCurrentConsoleLine(_buffer.Length);
            Console.WriteLine(message);
            Console.Write(_buffer.ToString());
        });

        await base.StartAsync(cancellationToken);
    }

    private static void ClearCurrentConsoleLine(int width)
    {
        if (width == 0)
        {
            return;
        }

        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', width));
        Console.SetCursorPosition(0, Console.CursorTop);
    }
}
