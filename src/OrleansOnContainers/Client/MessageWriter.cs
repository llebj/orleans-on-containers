using Shared.Helpers;
using Shared.Messages;

namespace Client;

public static class MessageWriter
{
    private const string _systemId = "System";

    public static void WriteLine(IMessage message) => Console.WriteLine(message);

    public static void WriteSystemMessage(string message) => Console.WriteLine(MessageBuilder.Build(message, _systemId));
}
