using Client.Application.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Client.Application.Tests;

public class ObserverManagerTests
{
    private readonly NullLogger<ObserverManager> _logger = new();
    private readonly NullLogger<MessageStream> _messageStreamLogger = new();

    [Fact]
    public void GivenNoManagedObservers_WhenCreateObserverIsCalled_ReturnAChatObserver()
    {
        // Arrange
        var observerManager = new ObserverManager(_logger, BuildMessageStream());

        // Act
        var observer = observerManager.CreateObserver();

        // Assert
        Assert.NotNull(observer);
    }

    [Fact]
    public void GivenNoManagedObservers_WhenCreateObserverIsCalled_SetIsManagingObserverToTrue()
    {
        // Arrange
        var observerManager = new ObserverManager(_logger, BuildMessageStream());

        // Act
        _ = observerManager.CreateObserver();

        // Assert
        Assert.True(observerManager.IsManagingObserver);
    }

    [Fact]
    public void GivenAManagedObserver_WhenCreateObserverIsCalled_ThrowAnInvalidOperationException()
    {
        // Arrange
        var observerManager = new ObserverManager(_logger, BuildMessageStream());
        _ = observerManager.CreateObserver();

        // Act
        // Assert
        Assert.Throws<InvalidOperationException>(observerManager.CreateObserver);
    }

    [Fact]
    public void GivenAManagedObserver_WhenDestroyObserverIsCalled_SetIsManagingObserverToFalse()
    {
        // Arrange
        var observerManager = new ObserverManager(_logger, BuildMessageStream());
        _ = observerManager.CreateObserver();

        // Act
        observerManager.DestroyObserver();

        // Assert
        Assert.False(observerManager.IsManagingObserver);
    }

    [Fact]
    public void GivenAManagerWithAPreviouslyDestroyedObserver_WhenCreateObserverIsCalled_ReturnAChatObserver()
    {
        // Arrange
        var observerManager = new ObserverManager(_logger, BuildMessageStream());
        _ = observerManager.CreateObserver();
        observerManager.DestroyObserver();

        // Act
        var result = observerManager.CreateObserver();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenAManagerWithAPreviouslyDestroyedObserver_WhenDestroyObserverIsCalled_SetIsManagingObserverToFalse()
    {
        // Arrange
        var observerManager = new ObserverManager(_logger, BuildMessageStream());
        _ = observerManager.CreateObserver();
        observerManager.DestroyObserver();

        // Act
        observerManager.DestroyObserver();

        // Assert
        Assert.False(observerManager.IsManagingObserver);
    }

    private IMessageStreamInput BuildMessageStream()
    {
        var messageStream = new MessageStream(_messageStreamLogger);
        var (_, ReleaseKey) = messageStream.GetReader();
        messageStream.ReleaseReader(ReleaseKey);

        return messageStream;
    }
}
