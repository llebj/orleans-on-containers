using GrainInterfaces;

namespace Client;

public static class MessageWriter
{
    private const string _systemId = "System";

    public static void WriteLine(IMessage message) => Console.WriteLine(BuildMessage(message.SentAt, message.Message, message.Chat, message.Sender));

    public static void WriteSystemMessage(string message) => Console.WriteLine(BuildMessage(DateTimeOffset.Now, message, _systemId));

    private static string BuildMessage(DateTimeOffset time, string message, params object[] from) => $"[{time.ToLocalTime():HH:mm:ss} {string.Join('/', from)}] {message}";
}
