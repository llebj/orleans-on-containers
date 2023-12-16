namespace Client;

public class Result
{
    public Result(bool isSuccess)
    {
        IsSuccess = isSuccess;
        Message = string.Empty;
    }

    public Result(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }

    public bool IsSuccess { get; }

    public string Message { get; }

    public static Result Failure(string message) => new(false, message);

    public static Result Success() => new(true);
}
