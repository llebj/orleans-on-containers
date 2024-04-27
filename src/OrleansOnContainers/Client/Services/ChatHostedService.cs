using Client.Application;
using Client.Application.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Messages;
using System.Text;

namespace Client.Services;

internal class ChatHostedService : BackgroundService
{
    private readonly Guid _clientId = Guid.NewGuid();

    private readonly IChatAccessClient _chatAccessClient;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ChatHostedService> _logger;
    private readonly IMessageClient _messageClient;
    private readonly IMessageStreamOutput _messageStreamOutput;

    public ChatHostedService(
        IChatAccessClient chatClient,
        IHostApplicationLifetime lifetime,
        ILogger<ChatHostedService> logger,
        IMessageClient messageClient,
        IMessageStreamOutput messageStreamOutput)
    {
        _chatAccessClient = chatClient;
        _lifetime = lifetime;
        _logger = logger;
        _messageClient = messageClient;
        _messageStreamOutput = messageStreamOutput;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Executing hosted service.");
        MessageWriter.WriteSystemMessage("Welcome!");

        await RunLobby(_clientId, stoppingToken);

        _logger.LogInformation("Finished executing hosted service.");
        _lifetime.StopApplication();
    }

    private static async Task ReadMessages(InputHandler inputHandler, MessageStreamReader messageStreamReader, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in messageStreamReader.ReadMessages().WithCancellation(cancellationToken))
            {
                inputHandler.Write(message);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task RunChat(string chat, Guid clientId, CancellationToken cancellationToken)
    {
        var inputHandler = new InputHandler();
        var (Reader, ReleaseKey) = _messageStreamOutput.GetReader();
        // Create a new CTS so that the read operation can be cancelled and awaited before returning
        // to the calling code. This ensures that all messages are read from the message stream.
        var cancellationTokenSource = new CancellationTokenSource();
        var readMessages = ReadMessages(inputHandler, Reader, cancellationTokenSource.Token);

        while (!cancellationToken.IsCancellationRequested)
        {
            var keyInfo = Console.ReadKey(true);

            if ((keyInfo.Modifiers == ConsoleModifiers.Control && keyInfo.Key == ConsoleKey.Q) ||
                keyInfo.Key == ConsoleKey.Escape)
            {
                break;
            }
            else if (keyInfo.Key == ConsoleKey.Backspace)
            {
                inputHandler.RemoveCharacter();

                continue;
            }
            else if (keyInfo.Key == ConsoleKey.Enter)
            {
                var message = inputHandler.Read();
                var sendResult = await _messageClient.SendMessage(chat, clientId, message.Trim());

                if (!sendResult.IsSuccess)
                {
                    MessageWriter.WriteSystemMessage(sendResult.Message);
                }

                continue;
            }

            inputHandler.AddCharacter(keyInfo.KeyChar);
        }

        cancellationTokenSource.Cancel();
        await readMessages;
        cancellationTokenSource.Dispose();
        _messageStreamOutput.ReleaseReader(ReleaseKey);
    }

    private async Task RunLobby(Guid clientId, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var input = Console.ReadLine();

            if (input is null)
            {
                MessageWriter.WriteSystemMessage("Please enter a valid command.");

                continue;
            }

            var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (tokens.Length != 4 || !tokens[0].Equals("join", StringComparison.OrdinalIgnoreCase) || !tokens[2].Equals("as", StringComparison.OrdinalIgnoreCase)) 
            {
                MessageWriter.WriteSystemMessage("You can join a chat using the following command 'join {chat} as {screen name}'.");

                continue;
            }

            var chat = tokens[1];
            var screenName = tokens[3];
            var joinResult = await _chatAccessClient.JoinChat(chat, clientId, screenName);

            if (!joinResult.IsSuccess)
            {
                MessageWriter.WriteSystemMessage(joinResult.Message);

                continue;
            }

            Console.Clear();
            MessageWriter.WriteSystemMessage($"Joined {chat} as {screenName}.");

            await RunChat(chat, clientId, cancellationToken);

            _ = await _chatAccessClient.LeaveChat(chat, clientId);
            Console.Clear();
            MessageWriter.WriteSystemMessage($"Left {chat}.");
        }
    }
}

internal class InputHandler
{
    private readonly MessageBuilder _messageBuilder = new();
    // The lock is used to control access to both the console and the message builder, ensuring
    // that incoming messages via Write are not interpolated into any message that is currently
    // being built using AddCharacter, RemoveCharacter, or Read.
    private readonly object _inputLock = new();

    public void AddCharacter(char character)
    {
        // Keys such as arrow keys or function keys are represented by a '\0' character which is not
        // represented in the console. These null characters break the clear characters functionality
        // as they result in the number of characters in the console and the message builder being different.
        if (character == '\0')
        {
            return;
        }

        lock (_inputLock)
        {
            _messageBuilder.AddCharacter(character);

            if (Console.CursorLeft == Console.BufferWidth - 1)
            {
                // We are at the end of a line, so move to the next line after writing.
                Console.WriteLine(character);
            }
            else
            {
                Console.Write(character);
            }
        }
    }

    public string Read()
    {
        var message = string.Empty;

        lock (_inputLock)
        {
            ClearCharacters(_messageBuilder.Length);
            message = _messageBuilder.Flush();
        }

        return message;
    }

    public void RemoveCharacter()
    {
        lock (_inputLock)
        {
            if (_messageBuilder.Length == 0)
            {
                return;
            }

            _messageBuilder.RemoveCharacter();

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

    public void Write(IMessage message)
    {
        lock (_inputLock)
        {
            ClearCharacters(_messageBuilder.Length);
            MessageWriter.WriteLine(message);
            Console.Write(_messageBuilder.Message);
        }
    }

    // Adding the ability to move the cursor left and right will break this method.
    private static void ClearCharacters(int charactersRemaining)
    {
        if (charactersRemaining == 0)
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

        if (clearCount == charactersRemaining)
        {
            return;
        }

        ClearCharacters(charactersRemaining - clearCount);
    }

    private class MessageBuilder
    {
        private readonly StringBuilder _buffer = new();

        public int Length => _buffer.Length;

        public string Message => _buffer.ToString();

        public void AddCharacter(char character) => _buffer.Append(character);

        public string Flush()
        {
            var message = _buffer.ToString();
            _buffer.Clear();

            return message;
        }

        public void RemoveCharacter() => _buffer.Remove(_buffer.Length - 1, 1);
    }
}
