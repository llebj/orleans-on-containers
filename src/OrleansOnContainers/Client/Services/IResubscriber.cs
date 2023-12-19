namespace Client.Services;

public interface IResubscriber<T>
{
    Task Clear();

    Task Register(T state, Func<T, Task> timerDelegate);
}
