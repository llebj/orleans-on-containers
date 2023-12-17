namespace Client.Options;

public class ObserverManagerOptions
{
    public const string Key = "ObserverManager";

    /// <summary>
    /// Measured in seconds.
    /// </summary>
    public int RefreshPeriod { get; set; }
}
