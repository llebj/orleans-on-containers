using Shared.Helpers;

namespace Shared.Messages;

public static class SystemMessage
{
    public const string Id = "System";

    public static void WriteLine(string message) => Console.WriteLine(MessageBuilder.Build(message, Id));
}
