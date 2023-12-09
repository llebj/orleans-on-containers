namespace Client.Options;

public record ClientOptions
{
    public Guid ClientId { get; } = Guid.NewGuid();
}
