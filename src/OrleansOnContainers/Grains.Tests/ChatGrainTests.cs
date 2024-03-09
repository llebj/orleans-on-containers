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

public class ChatGrainTests : IClassFixture<TestClusterFixture>
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
        var message = "hello";
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .SetGrain(grainId)
            .WithSubscriber(expectedClientId, firstObserver)
            .WithSubscriber(Guid.NewGuid(), secondObserver)
            .Build();

        // Act
        await grain.SendMessage(expectedClientId, message);

        // Assert
        await firstObserver.Received().ReceiveMessage(
            Arg.Is<IMessage>(m => 
                m.Category == MessageCategory.User && 
                m.Chat == grainId && 
                m.ClientId == expectedClientId && 
                m.Message == message));
        await secondObserver.Received().ReceiveMessage(
            Arg.Is<IMessage>(m =>
                m.Category == MessageCategory.User &&
                m.Chat == grainId &&
                m.ClientId == expectedClientId &&
                m.Message == message));
    }

    [Fact]
    public async Task GivenASubscriber_WhenAnotherClientSubscribes_ThenNotifyTheExistingObserver()
    {
        // Arrange
        var grainId = nameof(GivenASubscriber_WhenAnotherClientSubscribes_ThenNotifyTheExistingObserver);
        var expectedClientId = Guid.NewGuid();
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .SetGrain(grainId)
            .WithSubscriber(Guid.NewGuid(), firstObserver)
            .Build();

        // Act
        await grain.Subscribe(expectedClientId, _cluster.GrainFactory.CreateObjectReference<IChatObserver>(secondObserver));

        // Assert
        await firstObserver.Received().ReceiveMessage(
            Arg.Is<IMessage>(m =>
                m.Category == MessageCategory.System &&
                m.Chat == grainId &&
                m.ClientId == expectedClientId));
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
            .SetGrain(grainId)
            .WithSubscriber(Guid.NewGuid(), firstObserver)
            .Build();

        // Act
        await grain.Subscribe(Guid.NewGuid(), _cluster.GrainFactory.CreateObjectReference<IChatObserver>(secondObserver));

        // Assert
        await secondObserver.DidNotReceiveWithAnyArgs().ReceiveMessage(Arg.Any<IMessage>());
    }

    [Fact]
    public async Task GivenMultipleSubscribers_WhenAnExistingSubscriberResubscribes_ThenDoNotNotifyTheOtherObservers()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscribers_WhenAnExistingSubscriberResubscribes_ThenDoNotNotifyTheOtherObservers);
        var clientOne = Guid.NewGuid();
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .SetGrain(grainId)
            .WithSubscriber(clientOne, firstObserver)
            .WithSubscriber(Guid.NewGuid(), secondObserver)
            .Build();

        // Act
        await grain.Subscribe(clientOne, _fixture.DefaultCluster.GrainFactory.CreateObjectReference<IChatObserver>(firstObserver));

        // Assert
        await secondObserver.DidNotReceiveWithAnyArgs().ReceiveMessage(Arg.Any<IMessage>());
    }

    [Fact]
    public async Task GivenMultipleSubscribers_WhenAnExistingSubscriberResubscribesWithANewObserverAndAMessageIsSent_ThenSendTheMessageToTheNewObserverInsteadOfTheOldOne()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscribers_WhenAnExistingSubscriberResubscribesWithANewObserverAndAMessageIsSent_ThenSendTheMessageToTheNewObserverInsteadOfTheOldOne);
        var clientOne = Guid.NewGuid();
        var clientTwo = Guid.NewGuid();
        var message = "hello";
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var thirdObserver = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .SetGrain(grainId)
            .WithSubscriber(clientOne, firstObserver)
            .WithSubscriber(clientTwo, secondObserver)
            .Build();

        // Act
        // Register a new observer instance under the existing "clientOne" subscription.
        await grain.Subscribe(clientOne, _fixture.DefaultCluster.GrainFactory.CreateObjectReference<IChatObserver>(thirdObserver));
        // Send a message from the second client (as it remains unchanged).
        await grain.SendMessage(clientTwo, message);

        // Assert
        await firstObserver.DidNotReceiveWithAnyArgs().ReceiveMessage(Arg.Any<IMessage>());
        await thirdObserver.Received().ReceiveMessage(
            Arg.Is<IMessage>(m =>
                m.Category == MessageCategory.User &&
                m.Chat == grainId &&
                m.ClientId == clientTwo &&
                m.Message == message));
    }

    [Fact]
    public async Task GivenMultipleSubscribers_WhenAClientUnsubscribes_ThenNotifyTheRemainingObserver()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscribers_WhenAClientUnsubscribes_ThenNotifyTheRemainingObserver);
        var clientOne = Guid.NewGuid();
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .SetGrain(grainId)
            .WithSubscriber(clientOne, firstObserver)
            .WithSubscriber(Guid.NewGuid(), secondObserver)
            .Build();

        // Act
        await grain.Unsubscribe(clientOne);

        // Assert
        await secondObserver.Received().ReceiveMessage(
            Arg.Is<IMessage>(m =>
                m.Category == MessageCategory.System &&
                m.Chat == grainId &&
                m.ClientId == clientOne));
    }

    [Fact]
    public async Task GivenMultipleSubscribers_WhenAClientUnsubscribes_ThenDoNotNotifyTheUnsubscribedObserver()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscribers_WhenAClientUnsubscribes_ThenNotifyTheRemainingObserver);
        var clientOne = Guid.NewGuid();
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .SetGrain(grainId)
            .WithSubscriber(clientOne, firstObserver)
            .WithSubscriber(Guid.NewGuid(), secondObserver)
            .Build();

        // Act
        await grain.Unsubscribe(clientOne);

        // Assert
        await firstObserver.DidNotReceiveWithAnyArgs().ReceiveMessage(Arg.Any<IMessage>());
    }

    [Fact]
    public async Task GivenMultipleSubscribers_WhenAnUnsubscribedClientUnsubscribes_ThenDoNotNotifyAnyObservers()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscribers_WhenAClientUnsubscribes_ThenNotifyTheRemainingObserver);
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .SetGrain(grainId)
            .WithSubscriber(Guid.NewGuid(), firstObserver)
            .WithSubscriber(Guid.NewGuid(), secondObserver)
            .Build();

        // Act
        await grain.Unsubscribe(Guid.NewGuid());

        // Assert
        await firstObserver.DidNotReceiveWithAnyArgs().ReceiveMessage(Arg.Any<IMessage>());
        await secondObserver.DidNotReceiveWithAnyArgs().ReceiveMessage(Arg.Any<IMessage>());
    }

    [Fact]
    public async Task GivenMultipleSubscribers_WhenAClientUnsubscribesAndAMessageIsSent_ThenDoNotSendAnyMoreMessagesToTheUnsubscribedObserver()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscribers_WhenAClientUnsubscribes_ThenNotifyTheRemainingObserver);
        var firstObserver = Substitute.For<IChatObserver>();
        var secondObserver = Substitute.For<IChatObserver>();
        var unsubscribedClient = Guid.NewGuid();
        var remainingClient = Guid.NewGuid();
        var grain = await _fixture
            .GetGrainBuilder()
            .SetGrain(grainId)
            .WithSubscriber(unsubscribedClient, firstObserver)
            .WithSubscriber(remainingClient, secondObserver)
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
            .SetGrain(grainId)
            .WithSubscriber(failedClient, firstObserver)
            .WithSubscriber(remainingClient, secondObserver)
            .Build();

        // Act
        // The first message results in the firstObserver throwing an exception.
        await grain.SendMessage(remainingClient, firstMessage);
        await grain.SendMessage(remainingClient, secondMessage);

        // Assert
        await firstObserver.DidNotReceive().ReceiveMessage(Arg.Is<IMessage>(m => m.Message == secondMessage));
    }

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
        // use 2/3 so that one interval does not expire an observer, but two intervals does.
        var interval = GivenMultipleSubscribers_WhenASubscriptionExpires_ThenDoNotSendAnyMoreMessagesToTheExpiredObserver_SiloConfiguration.ObserverTimeout * (2 / 3.0);

        var customCluster = TestClusterFixture
            .StartCustomCluster<GivenMultipleSubscribers_WhenASubscriptionExpires_ThenDoNotSendAnyMoreMessagesToTheExpiredObserver_SiloConfiguration>();
        var grain = await _fixture
            .GetGrainBuilder(customCluster)
            .SetGrain(grainId)
            .WithSubscriber(clientOne, firstObserver)
            .WithSubscriber(Guid.NewGuid(), secondObserver)
            .Build();

        // Act
        GivenMultipleSubscribers_WhenASubscriptionExpires_ThenDoNotSendAnyMoreMessagesToTheExpiredObserver_SiloConfiguration
            .TimeProvider.Advance(TimeSpan.FromSeconds(interval));
        // Resubscribe the first client to reset its timeout.
        await grain.Subscribe(clientOne, _cluster.GrainFactory.CreateObjectReference<IChatObserver>(firstObserver));
        // Advance time sufficiently to expire the second subscriber.
        GivenMultipleSubscribers_WhenASubscriptionExpires_ThenDoNotSendAnyMoreMessagesToTheExpiredObserver_SiloConfiguration
            .TimeProvider.Advance(TimeSpan.FromSeconds(interval));
        await grain.SendMessage(clientOne, message);

        // Assert
        await secondObserver.DidNotReceive().ReceiveMessage(Arg.Is<IMessage>(m => m.Message == message));
    }
}

public class TestClusterFixture : IDisposable
{
    public TestClusterFixture() => DefaultCluster.Deploy();

    public TestCluster DefaultCluster { get; } = new TestClusterBuilder()
        .AddSiloBuilderConfigurator<DefaultTestSiloConfiguration>()
        .AddClientBuilderConfigurator<TestClientConfiguration>()
        .Build();

    public void Dispose() => DefaultCluster.StopAllSilos();

    public GrainBuilder GetGrainBuilder() => new(DefaultCluster);

    public GrainBuilder GetGrainBuilder(TestCluster cluster) => new(cluster);

    public static TestCluster StartCustomCluster<T>() where T : ISiloConfigurator, new()
    {
        var cluster = new TestClusterBuilder()
            .AddSiloBuilderConfigurator<T>()
            .AddClientBuilderConfigurator<TestClientConfiguration>()
            .Build();
        cluster.Deploy();

        return cluster;
    }

    private class DefaultTestSiloConfiguration : ISiloConfigurator
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

    private class TestClientConfiguration : IClientBuilderConfigurator
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
}

public class GivenMultipleSubscribers_WhenASubscriptionExpires_ThenDoNotSendAnyMoreMessagesToTheExpiredObserver_SiloConfiguration : ISiloConfigurator
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

public class GrainBuilder
{
    private readonly TestCluster _cluster;
    private readonly ICollection<(Guid Id, IChatObserver Observer)> _observers = new List<(Guid Id, IChatObserver Observer)>();
    private IChatGrain? _grain;

    public GrainBuilder(TestCluster cluster)
    {
        _cluster = cluster;
    }

    public async Task<IChatGrain> Build()
    {
        if (_grain is null)
        {
            throw new InvalidOperationException("No grain has been set to build.");
        }

        foreach (var (Id, Observer) in _observers)
        {
            await _grain.Subscribe(Id, _cluster.GrainFactory.CreateObjectReference<IChatObserver>(Observer));
        }

        foreach (var (Id, Observer) in _observers)
        {
            Observer.ClearReceivedCalls();
        }

        return _grain;
    }

    public GrainBuilder SetGrain(string grainId)
    {
        _grain = _cluster.GrainFactory.GetGrain<IChatGrain>(grainId);

        return this;
    }

    public GrainBuilder WithSubscriber(Guid observerId, IChatObserver observer)
    {
        _observers.Add((observerId, observer));

        return this;
    }
}
