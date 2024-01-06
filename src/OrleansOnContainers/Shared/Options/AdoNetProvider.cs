namespace Shared.Options;

public record AdoNetProvider
{
    public string? ConnectionString { get; set; }

    public string? Invariant { get; set; }
}
