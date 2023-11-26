namespace Shared.Options;

public record SiloEndpointOptions
{
    public string? HostName { get; set; }

    public int Port { get; set; }
}
