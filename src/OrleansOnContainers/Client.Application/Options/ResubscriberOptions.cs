namespace Client.Application.Options;

public class ResubscriberOptions
{
    private int _refreshPeriod;
    public const string Key = "Resubscriber";

    /// <summary>
    /// Measured in seconds.
    /// </summary>
    public int RefreshPeriod 
    {
        get => _refreshPeriod;
        set
        {
            _refreshPeriod = value;
            RefreshTimePeriod = TimeSpan.FromSeconds(value);
        } 
    }

    public TimeSpan RefreshTimePeriod { get; private set; }
}
