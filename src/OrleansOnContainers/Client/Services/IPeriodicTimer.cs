namespace Client.Services;

public interface IPeriodicTimer
{
    Task Start();

    Task Stop();
}
