namespace Shared.Options;

public record PrimarySiloOptions
{
    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 11111;
}
