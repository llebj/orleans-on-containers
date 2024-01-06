namespace Silo.Options;

public record SiloOptions
{
    public const string Key = "Silo";

    public string? ClusteringProvider { get; set; }
}
