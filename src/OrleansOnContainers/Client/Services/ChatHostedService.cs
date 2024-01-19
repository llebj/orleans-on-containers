using Client.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Client.Services;

internal class ChatHostedService : BackgroundService
{
    private readonly Guid _clientId = Guid.NewGuid();

    // TODO: This class is doing way too much: split out the message building functionality.
    //       The locking can then all be handled in one place.
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
        var joinResult = await _chatService.Join(_chatId, _clientId);

        if (!joinResult.IsSuccess)
        {
            SystemMessage.WriteLine(joinResult.Message);
            _lifetime.StopApplication();

            return;
        }

        SystemMessage.WriteLine($"Joined {_chatId} as {_clientId}.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var keyInfo = Console.ReadKey(true);

            if ((keyInfo.Modifiers == ConsoleModifiers.Control && keyInfo.Key == ConsoleKey.Q) || 
                keyInfo.Key == ConsoleKey.Escape)
            {
                break;
            }
            else if (keyInfo.Key == ConsoleKey.Backspace)
            {
                RemoveCharacter();
                
                continue;
            }
            else if (keyInfo.Key == ConsoleKey.Enter)
            {
                var messageResult = await SendMessage();

                if (!messageResult.IsSuccess)
                {
                    SystemMessage.WriteLine(messageResult.Message);
                }

                continue;
            }

            WriteCharacter(keyInfo);
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
                ClearCharacters(_buffer.Length);
                Console.WriteLine(message);
                Console.Write(_buffer.ToString());
            }
        });

        await base.StartAsync(cancellationToken);
    }

    // Beware! Adding the ability to move the cursor left and right will break this method!
    private static void ClearCharacters(int charactarsRemaining)
    {
        if (charactarsRemaining == 0)
        {
            return;
        }

        var isLeftMost = Console.CursorLeft == 0;
        var clearCount = isLeftMost ?
            Console.BufferWidth :
            Console.CursorLeft;

        // If this method has been called recursively then the cursor will be positioned
        // at the left-most position and it is the previous line that needs to be cleared.
        Console.SetCursorPosition(0, isLeftMost ? Console.CursorTop - 1 : Console.CursorTop);
        Console.Write(new string(' ', clearCount));
        Console.SetCursorPosition(0, Console.CursorTop);

        if (clearCount == charactarsRemaining)
        {
            return;
        }

        ClearCharacters(charactarsRemaining - clearCount);
    }

    private async Task<Result> SendMessage()
    {
        var message = string.Empty;

        lock (_inputLock)
        {
            message = _buffer.ToString();
            ClearCharacters(_buffer.Length);
            _buffer.Clear();
        }

        return await _chatService.SendMessage(_clientId, message.Trim());
    }

    private void RemoveCharacter()
    {
        lock (_inputLock)
        {
            if (_buffer.Length == 0)
            {
                return;
            }

            _buffer.Remove(_buffer.Length - 1, 1);

            if (Console.CursorLeft == 0)
            {
                // We are at the left-most position, so remove the character at the end of the
                // previous line.
                Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop - 1);
                Console.WriteLine(' ');
                Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop - 1);
            }
            else
            {
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                Console.Write(' ');
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
            }
        }
    }

    private void WriteCharacter(ConsoleKeyInfo keyInfo)
    {
        // Keys such as arrow keys or function keys are represented by a '\0' character which is not
        // represented in the console. These null characters break the clear characters functionality
        // as they result in the number of characters in the console and the buffer being different.
        if (keyInfo.KeyChar == '\0') 
        {
            return;
        }

        lock (_inputLock)
        {
            _buffer.Append(keyInfo.KeyChar);

            if (Console.CursorLeft == Console.BufferWidth - 1)
            {
                // We are at the end of a line, so move to the next line after writing.
                Console.WriteLine(keyInfo.KeyChar);
            }
            else
            {
                Console.Write(keyInfo.KeyChar);
            }
        }
    }
}
