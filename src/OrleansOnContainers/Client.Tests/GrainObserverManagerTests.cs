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
        var manager = new GrainObserverManager(
            clusterClient,
            Substitute.For<IGrainFactory>(),
            _fixture.ObserverManagerOptions,
            new FakeTimeProvider(DateTimeOffset.UtcNow));

        // Act
        await manager.Subscribe(observer, grainId);

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
        var manager = new GrainObserverManager(
            clusterClient,
            Substitute.For<IGrainFactory>(),
            _fixture.ObserverManagerOptions,
            new FakeTimeProvider(DateTimeOffset.UtcNow));
        await manager.Subscribe(observer, "test");

        // Act
        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.Subscribe(observer, "test-2"));
    }

    [Fact]
    public async Task GivenAnActiveSubscription_WhenTheRefreshPeriodExpires_ThenResubscibeToTheSameGrain()
    {
        // Arrange
        var grainId = "test";
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        clusterClient
            .GetGrain<IChatGrain>(grainId)
            .Returns(grain);
        var observer = Substitute.For<IChatObserver>();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var manager = new GrainObserverManager(
            clusterClient,
            Substitute.For<IGrainFactory>(),
            _fixture.ObserverManagerOptions, 
            timeProvider);
        await manager.Subscribe(observer, grainId);

        // Act
        timeProvider.Advance(TimeSpan.FromSeconds(_fixture.RefreshPeriod));

        // Assert
        clusterClient
            .Received(2)
            .GetGrain<IChatGrain>(grainId);
        await grain
            .Received(2)
            .Subscribe(Arg.Any<IChatObserver>());
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
