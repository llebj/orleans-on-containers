using Client.Services;
using GrainInterfaces;
using NSubstitute;
using Xunit;

namespace Client.Tests;

public class GrainObserverManagerTests
{
    [Fact]
    public async Task GivenNoActiveSubscription_WhenAClientAttemptsToSubscribe_ThenRegisterTheSubscriptionWithTheGrain()
    {
        // Arrange
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        clusterClient
            .GetGrain<IChatGrain>(Arg.Any<string>())
            .Returns(grain);
        var observer = Substitute.For<IChatObserver>();
        var manager = new GrainObserverManager(clusterClient);

        // Act
        await manager.Subscribe(observer, "test");

        // Assert
        await grain.Received().Subscribe(observer);
    }

    [Fact]
    public async Task GivenAnActiveSubscription_WhenAClientAttemptsToSubscribe_ThenThrowAnInvalidOperationException()
    {
        // Arrange
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        clusterClient
            .GetGrain<IChatGrain>(Arg.Any<string>())
            .Returns(grain);
        var observer = Substitute.For<IChatObserver>();
        var manager = new GrainObserverManager(clusterClient);
        await manager.Subscribe(observer, "test");

        // Act
        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.Subscribe(observer, "test-2"));
    }
}
