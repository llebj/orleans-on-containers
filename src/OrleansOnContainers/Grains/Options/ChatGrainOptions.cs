namespace Grains.Options;

public class ChatGrainOptions
{
    public const string Key = "ChatGrain";

    /// <summary>
    /// Measured in seconds.
    /// </summary>
    public int ObserverTimeout { get; set; }
}
