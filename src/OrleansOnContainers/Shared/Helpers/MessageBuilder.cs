namespace Shared.Helpers;

public static class MessageBuilder
{
    public static string Build(string message, params object[] from) => $"[{string.Join('/', from)}] {message}";
}
