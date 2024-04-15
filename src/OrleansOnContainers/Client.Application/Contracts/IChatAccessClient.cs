namespace Client.Application.Contracts;

public interface IChatAccessClient
{
    Task<Result> JoinChat(string chat, Guid clientId, string screenName);

    Task<Result> LeaveChat(string chat, Guid clientId);
}
