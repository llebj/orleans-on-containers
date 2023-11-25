namespace Shared.Options;

internal record PrimarySiloOptions
{
    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 11111;
}
