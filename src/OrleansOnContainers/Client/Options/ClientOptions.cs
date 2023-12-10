namespace Client.Options;

public class ClientOptions
{
    public ClientOptions()
    {
        
    }

    public ClientOptions(Guid clientId)
    {
        ClientId = clientId;
    }

    public Guid ClientId { get; set; }
}
