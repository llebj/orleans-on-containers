namespace Client.Services;

internal interface IChatService
{
    Task<Result> Join(string chat, Guid clientId);

    Task<Result> SendMessage(string clientId, string message);
}
