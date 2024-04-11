namespace Client.Application.Contracts;

public interface IChatClient
{
    Task<Result> JoinChat(string chat, Guid clientId, string screenName);

    Task<Result> LeaveCurrentChat(Guid clientId);

    Task<Result> SendMessage(Guid clientId, string message);
}
