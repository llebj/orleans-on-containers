namespace Client.Services;

public interface IPeriodicTimer<T>
{
    Task Start(T state, Func<T, Task> timerDelegate);

    Task Stop();
}
