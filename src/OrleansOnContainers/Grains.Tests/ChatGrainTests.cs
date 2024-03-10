using GrainInterfaces;
using Grains.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Orleans.Serialization;
using Orleans.TestingHost;
using Shared.Messages;
using Xunit;

namespace Grains.Tests;

[CollectionDefinition(ClusterCollection.Name)]
public class ClusterCollection : ICollectionFixture<TestClusterFixture>
{
    public const string Name = nameof(ClusterCollection);
}

[Collection(ClusterCollection.Name)]
public class ChatGrainTests
{
    private readonly TestCluster _cluster;
    private readonly TestClusterFixture _fixture;

    public ChatGrainTests(TestClusterFixture fixture)
    {
        _cluster = fixture.DefaultCluster;
        _fixture = fixture;
    }

    [Fact]
    public async Task GivenAnySubscribers_WhenAMessageIsSentFromAnUnsubscribedClient_ThenThrowAnInvalidOperationException()
    {
        // Arrange
        var grainId = nameof(GivenAnySubscribers_WhenAMessageIsSentFromAnUnsubscribedClient_ThenThrowAnInvalidOperationException);
        var grain = _cluster.GrainFactory.GetGrain<IChatGrain>(grainId);

        // Act
        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => grain.SendMessage(Guid.NewGuid(), "hello"));
    }

    [Fact]
    public async Task GivenMultipleSubscribers_WhenAMessageIsSent_ThenAllTheObserversReceiveTheMessage()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscribers_WhenAMessageIsSent_ThenAllTheObserversReceiveTheMessage);
        var expectedClientId = Guid.NewGuid();
        var expectedScreenName = "client-1";
        var message = "hello";
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .GetGrain(grainId)
            .WithSubscriber(expectedClientId, expectedScreenName, firstObserver)
            .WithSubscriber(Guid.NewGuid(), "client-2", secondObserver)
            .Build();

        // Act
        await grain.SendMessage(expectedClientId, message);

        // Assert
        await firstObserver.Received().ReceiveMessage(
            Arg.Is<IMessage>(m => 
                m.Category == MessageCategory.User && 
                m.Chat == grainId && 
                m.ScreenName == expectedScreenName && 
                m.Message == message));
        await secondObserver.Received().ReceiveMessage(
            Arg.Is<IMessage>(m =>
                m.Category == MessageCategory.User &&
                m.Chat == grainId &&
                m.ScreenName == expectedScreenName &&
                m.Message == message));
    }

    [Fact]
    public async Task GivenASubscriber_WhenTheSameClientSubscribesAgain_ThenThrowAnInvalidOperationException()
    {
        // Arrange
        var grainId = nameof(GivenASubscriber_WhenTheSameClientSubscribesAgain_ThenThrowAnInvalidOperationException);
        var clientId = Guid.NewGuid();
        var screenName = "client-1";
        var observer = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .GetGrain(grainId)
            .WithSubscriber(clientId, screenName, observer)
            .Build();

        // Act
        // Assert
        var observerReference = _cluster.GrainFactory.CreateObjectReference<IChatObserver>(observer);
        await Assert.ThrowsAsync<InvalidOperationException>(() => grain.Subscribe(clientId, screenName, observerReference));
    }

    [Fact]
    public async Task GivenASubscriber_WhenAnotherClientSubscribes_ThenNotifyTheExistingObserver()
    {
        // Arrange
        var grainId = nameof(GivenASubscriber_WhenAnotherClientSubscribes_ThenNotifyTheExistingObserver);
        var expectedClientId = Guid.NewGuid();
        var expectedScreenName = "client-2";
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .GetGrain(grainId)
            .WithSubscriber(Guid.NewGuid(), "client-1", firstObserver)
            .Build();

        // Act
        await grain.Subscribe(expectedClientId, expectedScreenName, _cluster.GrainFactory.CreateObjectReference<IChatObserver>(secondObserver));

        // Assert
        await firstObserver.Received().ReceiveMessage(
            Arg.Is<IMessage>(m =>
                m.Category == MessageCategory.System &&
                m.Chat == grainId &&
                m.ScreenName == expectedScreenName));
    }

    [Fact]
    public async Task GivenASubscriber_WhenAnotherClientSubscribes_ThenDoNotNotifyTheNewObserver()
    {
        // Arrange
        var grainId = nameof(GivenASubscriber_WhenAnotherClientSubscribes_ThenDoNotNotifyTheNewObserver);
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .GetGrain(grainId)
            .WithSubscriber(Guid.NewGuid(), "client-1", firstObserver)
            .Build();

        // Act
        await grain.Subscribe(Guid.NewGuid(), "client-2", _cluster.GrainFactory.CreateObjectReference<IChatObserver>(secondObserver));

        // Assert
        await secondObserver.DidNotReceiveWithAnyArgs().ReceiveMessage(Arg.Any<IMessage>());
    }

    [Fact]
    public async Task GivenNoSubscribers_WhenAClientResubscribes_ThenThrowAnInvalidOperationException()
    {
        // Arrange
        var grainId = nameof(GivenNoSubscribers_WhenAClientResubscribes_ThenThrowAnInvalidOperationException);
        var grain = await _fixture
            .GetGrainBuilder()
            .GetGrain(grainId)
            .Build();

        // Act
        // Assert
        var observer = _cluster.GrainFactory.CreateObjectReference<IChatObserver>(Substitute.For<IChatObserver>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => grain.Resubscribe(Guid.NewGuid(), observer));
    }

    [Fact]
    public async Task GivenMultipleSubscribers_WhenAnExistingSubscriberResubscribes_ThenDoNotNotifyTheOtherObservers()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscribers_WhenAnExistingSubscriberResubscribes_ThenDoNotNotifyTheOtherObservers);
        var clientOne = Guid.NewGuid();
        var screenNameOne = "client-1";
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .GetGrain(grainId)
            .WithSubscriber(clientOne, screenNameOne, firstObserver)
            .WithSubscriber(Guid.NewGuid(), "client-2", secondObserver)
            .Build();

        // Act
        await grain.Resubscribe(clientOne, _fixture.DefaultCluster.GrainFactory.CreateObjectReference<IChatObserver>(firstObserver));

        // Assert
        await secondObserver.DidNotReceiveWithAnyArgs().ReceiveMessage(Arg.Any<IMessage>());
    }

    [Fact]
    public async Task GivenMultipleSubscribers_WhenAnExistingSubscriberResubscribesWithANewObserverAndAMessageIsSent_ThenSendTheMessageToTheNewObserverInsteadOfTheOldOne()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscribers_WhenAnExistingSubscriberResubscribesWithANewObserverAndAMessageIsSent_ThenSendTheMessageToTheNewObserverInsteadOfTheOldOne);
        var clientOne = Guid.NewGuid();
        var screenNameOne = "client-1";
        var clientTwo = Guid.NewGuid();
        var screenNameTwo = "client-2";
        var message = "hello";
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var thirdObserver = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .GetGrain(grainId)
            .WithSubscriber(clientOne, screenNameOne, firstObserver)
            .WithSubscriber(clientTwo, screenNameTwo, secondObserver)
            .Build();

        // Act
        // Register a new observer instance under the existing "clientOne" subscription.
        await grain.Resubscribe(clientOne, _fixture.DefaultCluster.GrainFactory.CreateObjectReference<IChatObserver>(thirdObserver));
        // Send a message from the second client (as it remains unchanged).
        await grain.SendMessage(clientTwo, message);

        // Assert
        await firstObserver.DidNotReceiveWithAnyArgs().ReceiveMessage(Arg.Any<IMessage>());
        await thirdObserver.Received().ReceiveMessage(
            Arg.Is<IMessage>(m =>
                m.Category == MessageCategory.User &&
                m.Chat == grainId &&
                m.ScreenName == screenNameTwo &&
                m.Message == message));
    }

    [Fact]
    public async Task GivenMultipleSubscribers_WhenAClientUnsubscribes_ThenNotifyTheRemainingObserver()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscribers_WhenAClientUnsubscribes_ThenNotifyTheRemainingObserver);
        var clientOne = Guid.NewGuid();
        var screenNameOne = "client-1";
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .GetGrain(grainId)
            .WithSubscriber(clientOne, screenNameOne, firstObserver)
            .WithSubscriber(Guid.NewGuid(), "client-2", secondObserver)
            .Build();

        // Act
        await grain.Unsubscribe(clientOne);

        // Assert
        await secondObserver.Received().ReceiveMessage(
            Arg.Is<IMessage>(m =>
                m.Category == MessageCategory.System &&
                m.Chat == grainId &&
                m.ScreenName == screenNameOne));
    }

    [Fact]
    public async Task GivenMultipleSubscribers_WhenAClientUnsubscribes_ThenDoNotNotifyTheUnsubscribedObserver()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscribers_WhenAClientUnsubscribes_ThenDoNotNotifyTheUnsubscribedObserver);
        var clientOne = Guid.NewGuid();
        var screenNameOne = "client-1";
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .GetGrain(grainId)
            .WithSubscriber(clientOne, screenNameOne, firstObserver)
            .WithSubscriber(Guid.NewGuid(), "client-2", secondObserver)
            .Build();

        // Act
        await grain.Unsubscribe(clientOne);

        // Assert
        await firstObserver.DidNotReceiveWithAnyArgs().ReceiveMessage(Arg.Any<IMessage>());
    }

    [Fact]
    public async Task GivenMultipleSubscribers_WhenAnUnsubscribedClientUnsubscribes_ThenThrowAnInvalidOperationException()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscribers_WhenAnUnsubscribedClientUnsubscribes_ThenThrowAnInvalidOperationException);
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .GetGrain(grainId)
            .WithSubscriber(Guid.NewGuid(), "client-1", firstObserver)
            .WithSubscriber(Guid.NewGuid(), "client-2", secondObserver)
            .Build();

        // Act
        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => grain.Unsubscribe(Guid.NewGuid()));
    }

    [Fact]
    public async Task GivenMultipleSubscribers_WhenAClientUnsubscribesAndAMessageIsSent_ThenDoNotSendAnyMoreMessagesToTheUnsubscribedObserver()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscribers_WhenAClientUnsubscribesAndAMessageIsSent_ThenDoNotSendAnyMoreMessagesToTheUnsubscribedObserver);
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var unsubscribedClient = Guid.NewGuid();
        var remainingClient = Guid.NewGuid();
        var grain = await _fixture
            .GetGrainBuilder()
            .GetGrain(grainId)
            .WithSubscriber(unsubscribedClient, "client-1", firstObserver)
            .WithSubscriber(remainingClient, "client-2", secondObserver)
            .Build();

        // Act
        await grain.Unsubscribe(unsubscribedClient);
        await grain.SendMessage(remainingClient, "hello");

        // Assert
        await firstObserver.DidNotReceiveWithAnyArgs().ReceiveMessage(Arg.Any<IMessage>());
    }

    [Fact]
    public async Task GivenMultipleSubscribers_WhenAMessageIsSentAndOneObserverFails_ThenDoNotSendAnyMoreMessagesToTheFailedObserver()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscribers_WhenAMessageIsSentAndOneObserverFails_ThenDoNotSendAnyMoreMessagesToTheFailedObserver);
        var firstMessage = "first";
        var secondMessage = "second";
        var firstObserver = Substitute.For<IChatObserver>();
        firstObserver
            .ReceiveMessage(Arg.Is<IMessage>(m => m.Message == firstMessage))
            .Returns(x => { throw new Exception(); });
        var secondObserver = Substitute.For<IChatObserver>();
        var failedClient = Guid.NewGuid();
        var remainingClient = Guid.NewGuid();
        var grain = await _fixture
            .GetGrainBuilder()
            .GetGrain(grainId)
            .WithSubscriber(failedClient, "client-1", firstObserver)
            .WithSubscriber(remainingClient, "client-2", secondObserver)
            .Build();

        // Act
        // The first message results in the firstObserver throwing an exception.
        await grain.SendMessage(remainingClient, firstMessage);
        await grain.SendMessage(remainingClient, secondMessage);

        // Assert
        await firstObserver.DidNotReceive().ReceiveMessage(Arg.Is<IMessage>(m => m.Message == secondMessage));
    }
}

public class ExpirationTests
{
    // There is nothing explicit on the IChatGrain interface to suggest that subscriptions can expire, so this test is technically testing
    // an implementation detail rather than the interface. However, as stated in the Orleans documentation, observers are considered to be
    // unreliable, so it is reasonable to expect that any implementation will have a mechanism to remove defunct observers. Therefore, I
    // believe in this case it is acceptable to compromise and to test the implementation directly.
    [Fact]
    public async Task GivenMultipleSubscribers_WhenASubscriptionExpires_ThenDoNotSendAnyMoreMessagesToTheExpiredObserver()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscribers_WhenASubscriptionExpires_ThenDoNotSendAnyMoreMessagesToTheExpiredObserver);
        var message = "hello";
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var clientOne = Guid.NewGuid();
        var screenNameOne = "client-1";
        // use 2/3 so that one interval does not expire an observer, but two intervals does.
        var interval = ExpirationTestsSiloConfiguration.ObserverTimeout * (2 / 3.0);

        var cluster = new TestClusterBuilder()
            .AddSiloBuilderConfigurator<ExpirationTestsSiloConfiguration>()
            .AddClientBuilderConfigurator<TestClientConfiguration>()
            .Build();
        cluster.Deploy();

        var grain = await GrainBuilder
            .ForCluster(cluster)
            .GetGrain(grainId)
            .WithSubscriber(clientOne, screenNameOne, firstObserver)
            .WithSubscriber(Guid.NewGuid(), "client-2", secondObserver)
            .Build();

        // Act
        ExpirationTestsSiloConfiguration.TimeProvider.Advance(TimeSpan.FromSeconds(interval));
        // Resubscribe the first client to reset its timeout.
        await grain.Resubscribe(clientOne, cluster.GrainFactory.CreateObjectReference<IChatObserver>(firstObserver));
        // Advance time sufficiently to expire the second subscriber.
        ExpirationTestsSiloConfiguration.TimeProvider.Advance(TimeSpan.FromSeconds(interval));
        await grain.SendMessage(clientOne, message);

        // Assert
        await secondObserver.DidNotReceive().ReceiveMessage(Arg.Is<IMessage>(m => m.Message == message));
    }

    private class ExpirationTestsSiloConfiguration : ISiloConfigurator
    {
        private static readonly FakeTimeProvider _timeProvider = new();

        public static FakeTimeProvider TimeProvider => _timeProvider;

        public static int ObserverTimeout => 100;

        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<TimeProvider>(_timeProvider);
                services.Configure<ChatGrainOptions>(x => x.ObserverTimeout = ObserverTimeout);
            });
            siloBuilder.Services.AddSerializer(serializerBuilder =>
            {
                serializerBuilder.AddJsonSerializer(
                    isSupported: type => type.Namespace!.StartsWith("Shared"));
            });
        }
    }
}

[Collection(ClusterCollection.Name)]
public class ScreenNameTests
{
    private readonly TestCluster _cluster;
    private readonly TestClusterFixture _fixture;

    public ScreenNameTests(TestClusterFixture fixture)
    {
        _cluster = fixture.DefaultCluster;
        _fixture = fixture;
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("client-1", false)]
    [InlineData("client-2", true)]
    public async Task GivenAnExistingSubscriber_WhenAScreenNameIsChecked_ThenReturnTheAvailability(string newScreenName, bool expectedAvailability)
    {
        // Arrange
        var grainId = $"{nameof(GivenAnExistingSubscriber_WhenAScreenNameIsChecked_ThenReturnTheAvailability)}_{newScreenName}";
        var grain = await _fixture
            .GetGrainBuilder()
            .GetGrain(grainId)
            .WithSubscriber(Guid.NewGuid(), "client-1", Substitute.For<IChatObserver>())
            .Build();

        // Act
        var actualAvailability = await grain.ScreenNameIsAvailable(newScreenName);

        // Assert
        Assert.Equal(expectedAvailability, actualAvailability);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("client-1")]
    public async Task GivenAnExistingSubscriber_WhenANewClientSubscribesWithAnUnavailableScreenName_ThenThrowAnArgumentException(string newScreenName)
    {
        // Arrange
        var grainId = $"{nameof(GivenAnExistingSubscriber_WhenANewClientSubscribesWithAnUnavailableScreenName_ThenThrowAnArgumentException)}_{newScreenName}";
        var grain = await _fixture
            .GetGrainBuilder()
            .GetGrain(grainId)
            .WithSubscriber(Guid.NewGuid(), "client-1", Substitute.For<IChatObserver>())
            .Build();

        // Act
        // Assert
        var observer = _cluster.GrainFactory.CreateObjectReference<IChatObserver>(Substitute.For<IChatObserver>());
        await Assert.ThrowsAsync<ArgumentException>(() => grain.Subscribe(Guid.NewGuid(), newScreenName, observer));
    }
}

public class TestClusterFixture : IDisposable
{
    public TestClusterFixture() => DefaultCluster.Deploy();

    public TestCluster DefaultCluster { get; } = new TestClusterBuilder()
        .AddSiloBuilderConfigurator<TestSiloConfiguration>()
        .AddClientBuilderConfigurator<TestClientConfiguration>()
        .Build();

    public void Dispose() => DefaultCluster.StopAllSilos();

    public GrainBuilder GetGrainBuilder() => GrainBuilder.ForCluster(DefaultCluster);
}

public class TestSiloConfiguration : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<TimeProvider, FakeTimeProvider>();
            services.Configure<ChatGrainOptions>(x => x.ObserverTimeout = int.MaxValue);
        });
        siloBuilder.Services.AddSerializer(serializerBuilder =>
        {
            serializerBuilder.AddJsonSerializer(
                isSupported: type => type.Namespace!.StartsWith("Shared"));
        });
    }
}

public class TestClientConfiguration : IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
    {
        clientBuilder.Services.AddSerializer(serializerBuilder =>
        {
            serializerBuilder.AddJsonSerializer(
                isSupported: type => type.Namespace!.StartsWith("Shared"));
        });
    }
}

public class GrainBuilder
{
    private readonly TestCluster _cluster;
    private readonly List<(Guid Id, string ScreenName, IChatObserver Observer)> _subscribers = [];
    private IChatGrain? _grain;

    private GrainBuilder(TestCluster cluster)
    {
        _cluster = cluster;
    }

    public async Task<IChatGrain> Build()
    {
        if (_grain is null)
        {
            throw new InvalidOperationException("No grain has been set to build.");
        }

        foreach (var (Id, ScreenName, Observer) in _subscribers)
        {
            await _grain.Subscribe(Id, ScreenName, _cluster.GrainFactory.CreateObjectReference<IChatObserver>(Observer));
        }

        foreach (var (Id, ScreenName, Observer) in _subscribers)
        {
            Observer.ClearReceivedCalls();
        }

        return _grain;
    }

    public static GrainBuilder ForCluster(TestCluster cluster) => new(cluster);

    public GrainBuilder GetGrain(string grainId)
    {
        _grain = _cluster.GrainFactory.GetGrain<IChatGrain>(grainId);

        return this;
    }

    public GrainBuilder WithSubscriber(Guid clientId, string screenName, IChatObserver observer)
    {
        _subscribers.Add((clientId, screenName, observer));

        return this;
    }
}
