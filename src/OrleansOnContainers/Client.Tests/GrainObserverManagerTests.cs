using Client.Options;
using Client.Services;
using GrainInterfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Client.Tests;

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
            clusterClient,
            grainFactory,
            Substitute.For<IResubscriber<GrainObserverManagerState>>());

        // Act
        await manager.Subscribe(observer, grainId);

        // Assert
        await grain.Received().Subscribe(observerReference);
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
            clusterClient,
            grainFactory,
            Substitute.For<IResubscriber<GrainObserverManagerState>>());

        // Act
        // Catch and ignore the configured exception throw on the first call to subscribe.
        try
        {
            await manager.Subscribe(observer, grainId);
        }
        catch { }

        // Assert
        // The second call to subscribe should complete without issue.
        await manager.Subscribe(observer, grainId);
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
        var manager = new GrainObserverManager(
            clusterClient,
            Substitute.For<IGrainFactory>(),
            Substitute.For<IResubscriber<GrainObserverManagerState>>());
        await manager.Subscribe(observer, "test");

        // Act
        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.Subscribe(observer, "test-2"));
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
            clusterClient,
            grainFactory,
            Substitute.For<IResubscriber<GrainObserverManagerState>>());
        await manager.Subscribe(observer, grainId);

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
            clusterClient,
            grainFactory,
            Substitute.For<IResubscriber<GrainObserverManagerState>>());
        await manager.Subscribe(observer, grainId);

        // Act
        await manager.Unsubscribe(observer, grainId);

        // Assert
        await grain.Received().Unsubscribe(observerReference);
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
            clusterClient,
            grainFactory,
            Substitute.For<IResubscriber<GrainObserverManagerState>>());
        await manager.Subscribe(observer, grainId);
        timeProvider.Advance(TimeSpan.FromSeconds(_fixture.RefreshPeriod));

        // Act
        await manager.Unsubscribe(observer, grainId);
        // Advance the time provider again to ensure that if the client had not unsubscribed
        // then more calls to resubscribe would have been made in addition to the initial one.
        timeProvider.Advance(TimeSpan.FromSeconds(2 * _fixture.RefreshPeriod));

        // Assert
        await grain
            .Received(2)
            .Subscribe(observerReference);
    }

    [Fact]
    public async Task GivenAnUnsubscribedClient_WhenTheClientSubscribes_ThenRegisterTheNewSubscriptionWithTheGrain()
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
            clusterClient,
            grainFactory,
            Substitute.For<IResubscriber<GrainObserverManagerState>>());
        await manager.Subscribe(observer, grainId);
        await manager.Unsubscribe(observer, grainId);

        // Act
        await manager.Subscribe(observer, grainId);

        // Assert
        await grain.Received().Subscribe(observerReference);
    }

    [Fact]
    public async Task GivenNoActiveSubscription_WhenAClientUnsubscribes_ThenThrowAnInvalidOperationException()
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
            clusterClient,
            Substitute.For<IGrainFactory>(),
            Substitute.For<IResubscriber<GrainObserverManagerState>>());

        // Act
        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.Unsubscribe(observer, grainId));
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
