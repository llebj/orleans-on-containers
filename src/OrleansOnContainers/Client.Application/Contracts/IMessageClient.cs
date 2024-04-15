namespace Client.Application.Contracts;

public interface IMessageClient
{
    Task<Result> SendMessage(string chat, Guid clientId, string message);
}
