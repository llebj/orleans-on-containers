using GrainInterfaces;

namespace Client;

public static class MessageWriter
{
    private const string _systemId = "System";

    public static void WriteLine(IMessage message) => Console.WriteLine(BuildMessage(message.Message, message.Chat, message.Sender));

    public static void WriteSystemMessage(string message) => Console.WriteLine(BuildMessage(message, _systemId));

    private static string BuildMessage(string message, params object[] from) => $"[{string.Join('/', from)}] {message}";
}
