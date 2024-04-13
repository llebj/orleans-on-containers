namespace Client.Application.Contracts;

public interface IChatClient
{
    Task<Result> JoinChat(string chat, Guid clientId, string screenName);

    Task<Result> LeaveChat(string chat, Guid clientId);

    Task<Result> SendMessage(string chat, Guid clientId, string message);
}
