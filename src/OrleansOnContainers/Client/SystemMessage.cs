using Shared.Helpers;

namespace Client;

public static class SystemMessage
{
    private const string _system = "System";

    public static void WriteLine(string message) => Console.WriteLine(MessageBuilder.Build(message, _system));
}
