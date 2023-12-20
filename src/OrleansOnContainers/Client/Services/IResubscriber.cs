namespace Client.Services;

/// <summary>
/// Client interface for configuring the re-subscription of grain observers.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IResubscriber<T>
{
    /// <summary>
    /// Stops the grain observer re-subscription.
    /// </summary>
    /// <returns></returns>
    Task Clear();

    /// <summary>
    /// Registers a delegate that is used to resubscribe a grain observer.
    /// </summary>
    /// <param name="state">Information required to re-subscribe a grain observer.</param>
    /// <param name="timerDelegate">A delegate that subscribes a grain observer.</param>
    /// <returns></returns>
    Task Register(T state, Func<T, Task> timerDelegate);
}
