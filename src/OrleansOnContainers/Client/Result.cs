namespace Client;

public class Result
{
    private Result(bool isSuccess)
    {
        IsSuccess = isSuccess;
        Message = string.Empty;
    }

    private Result(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }

    public bool IsSuccess { get; }

    public string Message { get; }

    public static Result Failure(string message) => new(false, message);

    public static Result Success() => new(true);

    public static Result Success(string message) => new(true, message);
}
