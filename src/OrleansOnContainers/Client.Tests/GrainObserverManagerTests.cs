using Client.Services;
using GrainInterfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Client.Tests;

// #1_mil_subs
// TODO: Test setup in this class is nasty. Builder methods could be used to refine the process.
public class GrainObserverManagerTests
{
    [Fact]
    public async Task GivenNoActiveSubscription_WhenAClientSuccessfullySubscribes_ThenReturnASuccessResult()
    {
        // Arrange
        var grainId = "test";
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        clusterClient
            .GetGrain<IChatGrain>(grainId)
            .Returns(grain);
        var observer = Substitute.For<IChatObserver>();
        var observerReference = Substitute.For<IChatObserver>();
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory
            .CreateObjectReference<IChatObserver>(observer)
            .Returns(observerReference);
        var manager = new GrainObserverManager(
            observer,
            clusterClient,
            grainFactory,
            new NullLogger<GrainObserverManager>(),
            Substitute.For<IResubscriber<GrainSubscription>>());

        // Act
        var result = await manager.Subscribe(grainId);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GivenNoActiveSubscription_WhenTheGrainFactoryThrows_ThenReturnAFailureResult()
    {
        // Arrange
        var grainId = "test";
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        clusterClient
            .GetGrain<IChatGrain>(grainId)
            .Returns(grain);
        var observer = Substitute.For<IChatObserver>();
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory
            .CreateObjectReference<IChatObserver>(observer)
            .Returns(x => { throw new Exception(); });
        var manager = new GrainObserverManager(
            observer,
            clusterClient,
            grainFactory,
            new NullLogger<GrainObserverManager>(),
            Substitute.For<IResubscriber<GrainSubscription>>());

        // Act
        var result = await manager.Subscribe(grainId);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GivenNoActiveSubscription_WhenASubscriptionFails_ThenReturnAFailureResult()
    {
        // Arrange
        var grainId = "test";
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        grain
            .Subscribe(grainId, Arg.Any<IChatObserver>())
            .Returns(x => { throw new Exception(); });
        clusterClient
            .GetGrain<IChatGrain>(grainId)
            .Returns(grain);
        var observer = Substitute.For<IChatObserver>();
        var observerReference = Substitute.For<IChatObserver>();
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory
            .CreateObjectReference<IChatObserver>(observer)
            .Returns(observerReference);
        var manager = new GrainObserverManager(
            observer,
            clusterClient,
            grainFactory,
            new NullLogger<GrainObserverManager>(),
            Substitute.For<IResubscriber<GrainSubscription>>());

        // Act
        var result = await manager.Subscribe(grainId);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GivenNoActiveSubscription_WhenResubscriptionRegistrationFails_ThenReturnASuccessResult()
    {
        // Arrange
        var grainId = "test";
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        clusterClient
            .GetGrain<IChatGrain>(grainId)
            .Returns(grain);
        var observer = Substitute.For<IChatObserver>();
        var observerReference = Substitute.For<IChatObserver>();
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory
            .CreateObjectReference<IChatObserver>(observer)
            .Returns(observerReference);
        var resubscriber = Substitute.For<IResubscriber<GrainSubscription>>();
        resubscriber
            .Register(new GrainSubscription(grainId, observerReference), x => Task.CompletedTask)
            .ReturnsForAnyArgs(x => { throw new Exception(); });
        var manager = new GrainObserverManager(
            observer,
            clusterClient,
            grainFactory,
            new NullLogger<GrainObserverManager>(),
            resubscriber);

        // Act
        var result = await manager.Subscribe(grainId);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GivenNoActiveSubscription_WhenASubscriptionFails_ThenASecondSuccessfulSubscriptionCanBeMade()
    {
        // Arrange
        var grainId = "test";
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        // Fail the first subscription call to the grain.
        grain
            .Subscribe(grainId, Arg.Any<IChatObserver>())
            .Returns(
                x => { throw new Exception(); },
                x => Task.CompletedTask);
        clusterClient
            .GetGrain<IChatGrain>(grainId)
            .Returns(grain);
        var observer = Substitute.For<IChatObserver>();
        var observerReference = Substitute.For<IChatObserver>();
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory
            .CreateObjectReference<IChatObserver>(observer)
            .Returns(observerReference);
        var manager = new GrainObserverManager(
            observer,
            clusterClient,
            grainFactory,
            new NullLogger<GrainObserverManager>(),
            Substitute.For<IResubscriber<GrainSubscription>>());

        // Act
        // Catch and ignore the configured exception throw on the first call to subscribe.
        try
        {
            await manager.Subscribe(grainId);
        }
        catch { }

        var result = await manager.Subscribe(grainId);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GivenAnActiveSubscription_WhenAClientAttemptsToSubscribe_ThenReturnAFailureResult()
    {
        // Arrange
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        clusterClient
            .GetGrain<IChatGrain>(Arg.Any<string>())
            .Returns(grain);
        var observer = Substitute.For<IChatObserver>();
        var manager = new GrainObserverManager(
            observer,
            clusterClient,
            Substitute.For<IGrainFactory>(),
            new NullLogger<GrainObserverManager>(),
            Substitute.For<IResubscriber<GrainSubscription>>());
        await manager.Subscribe("test");

        // Act
        var result = await manager.Subscribe("test-2");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GivenAnActiveSubscription_WhenAClientSuccessfullyUnsubscribes_ThenReturnASuccessResult()
    {
        // Arrange
        var grainId = "test";
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        clusterClient
            .GetGrain<IChatGrain>(grainId)
            .Returns(grain);
        var observer = Substitute.For<IChatObserver>();
        var observerReference = Substitute.For<IChatObserver>();
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory
            .CreateObjectReference<IChatObserver>(observer)
            .Returns(observerReference);
        var resubscriber = Substitute.For<IResubscriber<GrainSubscription>>();
        var manager = new GrainObserverManager(
            observer,
            clusterClient,
            grainFactory,
            new NullLogger<GrainObserverManager>(),
            resubscriber);
        await manager.Subscribe( grainId);

        // Act
        var result = await manager.Unsubscribe(grainId);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GivenAnUnsubscribedClient_WhenTheClientSuccessfullySubscribesAgain_ThenReturnASuccessResult()
    {
        // Arrange
        var grainId = "test";
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        clusterClient
            .GetGrain<IChatGrain>(grainId)
            .Returns(grain);
        var observer = Substitute.For<IChatObserver>();
        var observerReference = Substitute.For<IChatObserver>();
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory
            .CreateObjectReference<IChatObserver>(observer)
            .Returns(observerReference);
        var manager = new GrainObserverManager(
            observer,
            clusterClient,
            grainFactory,
            new NullLogger<GrainObserverManager>(),
            Substitute.For<IResubscriber<GrainSubscription>>());
        await manager.Subscribe(grainId);
        await manager.Unsubscribe(grainId);

        // Act
        var result = await manager.Subscribe(grainId);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GivenNoActiveSubscription_WhenAClientUnsubscribes_ThenReturnAFailureResult()
    {
        // Arrange
        var grainId = "test";
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        clusterClient
            .GetGrain<IChatGrain>(grainId)
            .Returns(grain);
        var observer = Substitute.For<IChatObserver>();
        var manager = new GrainObserverManager(
            observer,
            clusterClient,
            Substitute.For<IGrainFactory>(),
            new NullLogger<GrainObserverManager>(),
            Substitute.For<IResubscriber<GrainSubscription>>());

        // Act
        var result = await manager.Unsubscribe(grainId);

        // Assert
        Assert.False(result.IsSuccess);
    }
}
