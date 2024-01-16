using Client.Options;
using Client.Services;
using GrainInterfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Client.Tests;

// #1_mil_subs
public class GrainObserverManagerTests : IClassFixture<GrainObserverManagerTestsFixture>
{
    private readonly GrainObserverManagerTestsFixture _fixture;

    public GrainObserverManagerTests(GrainObserverManagerTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GivenNoActiveSubscription_WhenAClientAttemptsToSubscribe_ThenRegisterTheSubscriptionWithTheGrain()
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
        await manager.Subscribe(grainId);

        // Assert
        await grain.Received().Subscribe(observerReference);
    }

    [Fact]
    public async Task GivenNoActiveSubscription_WhenAClientSubscribes_ThenRegisterTheClientForResubscription()
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

        // Act
        await manager.Subscribe(grainId);

        // Assert
        await resubscriber
            .Received()
            .Register(
                Arg.Is<GrainSubscription>(x => x.GrainId == grainId && x.Reference == observerReference), 
                Arg.Any<Func<GrainSubscription, Task>>());
    }

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
    public async Task GivenNoActiveSubscription_WhenASubscriptionFails_ThenASecondSuccessfulSubscriptionCanBeMade()
    {
        // Arrange
        var grainId = "test";
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        // Fail the first subscription call to the grain.
        grain
            .Subscribe(Arg.Any<IChatObserver>())
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
    public async Task GivenAnActiveSubscription_WhenAClientAttemptsToSubscribe_ThenReturnAFailureResultWithAMessage()
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
        Assert.False(string.IsNullOrEmpty(result.Message));
    }

    [Fact(Skip = "No longer relevant (functionality abstracted).")]
    public async Task GivenAnActiveSubscription_WhenTheRefreshPeriodExpires_ThenResubscibeToTheGrain()
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
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var manager = new GrainObserverManager(
            observer,
            clusterClient,
            grainFactory,
            new NullLogger<GrainObserverManager>(),
            Substitute.For<IResubscriber<GrainSubscription>>());
        await manager.Subscribe(grainId);

        // Act
        timeProvider.Advance(TimeSpan.FromSeconds(_fixture.RefreshPeriod));

        // Assert
        await grain
            .Received(2)
            .Subscribe(observerReference);
    }

    [Fact]
    public async Task GivenAnActiveSubscription_WhenAClientUnsubscribes_ThenUnsubscribeFromTheGrain()
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

        // Act
        await manager.Unsubscribe(grainId);

        // Assert
        await grain.Received().Unsubscribe(observerReference);
    }

    [Fact]
    public async Task GivenAnActiveSubscription_WhenAClientUnsubscribes_ThenClearTheResubscriptionRegistration()
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
        await manager.Subscribe(grainId);

        // Act
        await manager.Unsubscribe(grainId);

        // Assert
        await resubscriber.Received().Clear();
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

    [Fact(Skip = "No longer relevant (functionality abstracted).")]
    public async Task GivenAnActiveSubscriptionThatResubscribes_WhenAClientUnsubscribes_ThenStopResubscribingToTheGrain()
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
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var manager = new GrainObserverManager(
            observer,
            clusterClient,
            grainFactory,
            new NullLogger<GrainObserverManager>(),
            Substitute.For<IResubscriber<GrainSubscription>>());
        await manager.Subscribe(grainId);
        timeProvider.Advance(TimeSpan.FromSeconds(_fixture.RefreshPeriod));

        // Act
        await manager.Unsubscribe(grainId);
        // Advance the time provider again to ensure that if the client had not unsubscribed
        // then more calls to resubscribe would have been made in addition to the initial one.
        timeProvider.Advance(TimeSpan.FromSeconds(2 * _fixture.RefreshPeriod));

        // Assert
        await grain
            .Received(2)
            .Subscribe(observerReference);
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
    public async Task GivenNoActiveSubscription_WhenAClientUnsubscribes_ThenReturnAFailureResultWithAMessage()
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
        Assert.False(string.IsNullOrEmpty(result.Message));
    }
}

public class GrainObserverManagerTestsFixture 
{
    public GrainObserverManagerTestsFixture()
    {
        ObserverManagerOptions = MockOptions();
    }

    public IOptions<ObserverManagerOptions> ObserverManagerOptions { get; }

    public int RefreshPeriod { get; } = 150;

    private IOptions<ObserverManagerOptions> MockOptions()
    {
        var substitute = Substitute.For<IOptions<ObserverManagerOptions>>();
        var options = new ObserverManagerOptions
        {
            RefreshPeriod = RefreshPeriod
        };
        substitute.Value.Returns(options);

        return substitute;
    }
}
