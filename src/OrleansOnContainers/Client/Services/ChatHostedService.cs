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
            else if (keyInfo.Key == ConsoleKey.Backspace)
            {
                RemoveCharacter();
                
                continue;
            }
            else if (SupportedKeys.Alphanumeric.Contains(keyInfo.Key))
            {
                WriteCharacter(keyInfo);

                continue;
            }
            else if (keyInfo.Key != ConsoleKey.Enter)
            {
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

        // If this method has been called recursively then the cursor will be positioned
        // on top of the final character in the line. In this case there will be 
        // [cursorPosition + 1] characters in the current line, as opposed to [cursorPosition] characters.
        var clearCount = Console.CursorLeft == Console.BufferWidth - 1 ? 
            Console.CursorLeft + 1 :
            Console.CursorLeft;
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', clearCount));
        Console.SetCursorPosition(0, Console.CursorTop);

        if (clearCount == charactarsRemaining)
        {
            return;
        }

        // We need to continue clearing lines until there are no characters remaining.
        Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop - 1);
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

        return await _chatService.SendMessage(_options.ClientId, message.Trim());
    }

    private void RemoveCharacter()
    {
        lock (_inputLock)
        {
            if (_buffer.Length == 0)
            {
                // If the buffer is empty then there are no characters to remove so we can return.
                return;
            }

            _buffer.Remove(_buffer.Length - 1, 1);

            if (Console.CursorLeft == 0)
            {
                // If the cursor is at the left-most position then we know that the next character
                // to remove must be on the previous line, so we first move the cursor to the end
                // position on the previous line (we know that there are characters on the previous
                // line because the buffer contains characters.). We then write a blank character,
                // followed by a new-line, and then move the cursor back to the end of the previous line.
                Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop - 1);
                Console.WriteLine(' ');
                Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop - 1);
            }
            else
            {
                // If the cursor is not at the left-most position then we can simply replace the
                // previous chracter with a whitespace character and then move the cursor back one place.
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                Console.Write(' ');
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
            }
        }
    }

    private void WriteCharacter(ConsoleKeyInfo keyInfo)
    {
        lock (_inputLock)
        {
            _buffer.Append(keyInfo.KeyChar);

            if (Console.CursorLeft == Console.BufferWidth - 1)
            {
                // If the cursor is at the end position then we want to move the cursor onto the next
                // line after writing the character.
                Console.WriteLine(keyInfo.KeyChar);
            }
            else
            {
                Console.Write(keyInfo.KeyChar);
            }
        }
    }
}

internal static class SupportedKeys
{
    private readonly static HashSet<ConsoleKey> _alphabet = [
            ConsoleKey.A,
            ConsoleKey.B,
            ConsoleKey.C,
            ConsoleKey.D,
            ConsoleKey.E,
            ConsoleKey.F,
            ConsoleKey.G,
            ConsoleKey.H,
            ConsoleKey.I,
            ConsoleKey.J,
            ConsoleKey.K,
            ConsoleKey.L,
            ConsoleKey.M,
            ConsoleKey.N,
            ConsoleKey.O,
            ConsoleKey.P,
            ConsoleKey.Q,
            ConsoleKey.R,
            ConsoleKey.S,
            ConsoleKey.T,
            ConsoleKey.U,
            ConsoleKey.V,
            ConsoleKey.W,
            ConsoleKey.X,
            ConsoleKey.Y,
            ConsoleKey.Z
        ];

    private readonly static HashSet<ConsoleKey> _numbers = [
            ConsoleKey.D0,
            ConsoleKey.D1,
            ConsoleKey.D2,
            ConsoleKey.D3,
            ConsoleKey.D4,
            ConsoleKey.D5,
            ConsoleKey.D6,
            ConsoleKey.D7,
            ConsoleKey.D8,
            ConsoleKey.D9
        ];

    private readonly static HashSet<ConsoleKey> _alphanumeric = _alphabet.Union(_numbers).ToHashSet();

    public static HashSet<ConsoleKey> Alphanumeric => _alphanumeric;
}
