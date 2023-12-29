using Client.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Client.Services;

internal class ChatHostedService : BackgroundService
{
    // TODO: This class is doing way too much: split out console functionality.
    private readonly StringBuilder _buffer = new();
    private readonly string _chatId = "test";
    private readonly object _inputLock = new();

    private readonly IChatService _chatService;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ChatHostedService> _logger;
    private readonly IMessageStream _messageStream;
    private readonly ClientOptions _options;

    public ChatHostedService(
        IChatService chatService,
        IHostApplicationLifetime lifetime,
        ILogger<ChatHostedService> logger,
        IMessageStream messageStream,
        IOptions<ClientOptions> options)
    {
        _chatService = chatService;
        _lifetime = lifetime;
        _logger = logger;
        _messageStream = messageStream;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Executing hosted service.");
        var joinResult = await _chatService.Join(_chatId, _options.ClientId);

        if (!joinResult.IsSuccess)
        {
            SystemMessage.WriteLine(joinResult.Message);
            _lifetime.StopApplication();

            return;
        }

        SystemMessage.WriteLine($"Joined {_chatId} as {_options.ClientId}.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var keyInfo = Console.ReadKey(true);

            if ((keyInfo.Modifiers == ConsoleModifiers.Control && keyInfo.Key == ConsoleKey.Q) || 
                keyInfo.Key == ConsoleKey.Escape)
            {
                break;
            }
            else if (keyInfo.Key != ConsoleKey.Enter)
            {
                lock (_inputLock)
                {
                    Console.Write(keyInfo.KeyChar);
                    _buffer.Append(keyInfo.KeyChar);
                }

                continue;
            }

            var messageResult = await SendMessage();

            if (!messageResult.IsSuccess)
            {
                SystemMessage.WriteLine(messageResult.Message);
            }
        }

        SystemMessage.WriteLine($"Leaving {_chatId}.");
        _logger.LogInformation("Finished executing hosted service.");
        _lifetime.StopApplication();
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _ =_messageStream.Messages.Subscribe(message =>
        {
            lock (_inputLock)
            {
                ClearCurrentConsoleLine(_buffer.Length);
                Console.WriteLine(message);
                Console.Write(_buffer.ToString());
            }
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

    private async Task<Result> SendMessage()
    {
        var message = string.Empty;

        lock (_inputLock)
        {
            message = _buffer.ToString();
            ClearCurrentConsoleLine(_buffer.Length);
            _buffer.Clear();
        }

        return await _chatService.SendMessage(_options.ClientId, message.Trim());
    }
}
