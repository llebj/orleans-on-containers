namespace Client.Services;

internal interface IChatService
{
    Task<Result> Join(string chat, Guid clientId);

    Task<Result> SendMessage(Guid clientId, string message);
}
