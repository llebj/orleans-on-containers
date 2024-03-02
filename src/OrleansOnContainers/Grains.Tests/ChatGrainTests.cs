using GrainInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
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
        _cluster = fixture.Cluster;
        _fixture = fixture;
    }

    [Fact]
    public async Task GivenAnyObservers_WhenAMessageIsSentFromAnUnsubscribedClient_ThenThrowAnInvalidOperationException()
    {
        // Arrange
        var grainId = nameof(GivenAnyObservers_WhenAMessageIsSentFromAnUnsubscribedClient_ThenThrowAnInvalidOperationException);
        var grain = _cluster.GrainFactory.GetGrain<IChatGrain>(grainId);

        // Act
        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => grain.SendMessage("client", "hello"));
    }

    [Fact]
    public async Task GivenMultipleSubscribedObservers_WhenAMessageIsSentToTheGrain_ThenAllTheObserversReceiveTheMessage()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscribedObservers_WhenAMessageIsSentToTheGrain_ThenAllTheObserversReceiveTheMessage);
        var expectedClientId = "client-1";
        var message = "hello";
        var firstSubscriber = Substitute.For<IChatObserver>();
        var secondSubscriber = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .SetGrain(grainId)
            .WithSubscriber(expectedClientId, firstSubscriber)
            .WithSubscriber("client-2", secondSubscriber)
            .Build();

        // Act
        await grain.SendMessage(expectedClientId, message);

        // Assert
        await firstSubscriber.Received().ReceiveMessage(
            Arg.Is<IMessage>(m => 
                m.Category == MessageCategory.User && 
                m.Chat == grainId && 
                m.ClientId == expectedClientId && 
                m.Message == message));
        await secondSubscriber.Received().ReceiveMessage(
            Arg.Is<IMessage>(m =>
                m.Category == MessageCategory.User &&
                m.Chat == grainId &&
                m.ClientId == expectedClientId &&
                m.Message == message));
    }

    [Fact]
    public async Task GivenASubscribedObserver_WhenAnotherObserverSubscribes_ThenNotifyTheExistingObserver()
    {
        // Arrange
        var grainId = nameof(GivenASubscribedObserver_WhenAnotherObserverSubscribes_ThenNotifyTheExistingObserver);
        var expectedClientId = "client-2";
        var firstSubscriber = Substitute.For<IChatObserver>();
        var secondSubscriber = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .SetGrain(grainId)
            .WithSubscriber("client-1", firstSubscriber)
            .Build();

        // Act
        await grain.Subscribe(expectedClientId, _cluster.GrainFactory.CreateObjectReference<IChatObserver>(secondSubscriber));

        // Assert
        await firstSubscriber.Received().ReceiveMessage(
            Arg.Is<IMessage>(m =>
                m.Category == MessageCategory.System &&
                m.Chat == grainId &&
                m.ClientId == expectedClientId));
    }

    [Fact]
    public async Task GivenASubscribedObserver_WhenAnotherObserverSubscribes_ThenDoNotNotifyTheNewObserver()
    {
        // Arrange
        var grainId = nameof(GivenASubscribedObserver_WhenAnotherObserverSubscribes_ThenDoNotNotifyTheNewObserver);
        var firstSubscriber = Substitute.For<IChatObserver>();
        var secondSubscriber = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .SetGrain(grainId)
            .WithSubscriber("client-1", firstSubscriber)
            .Build();

        // Act
        await grain.Subscribe("client-2", _cluster.GrainFactory.CreateObjectReference<IChatObserver>(secondSubscriber));

        // Assert
        await secondSubscriber.DidNotReceiveWithAnyArgs().ReceiveMessage(Arg.Any<IMessage>());
    }

    [Fact]
    public async Task GivenMultipleSubscribedObservers_WhenAnExistingObserverResubscribes_ThenDoNotNotifyTheOtherObservers()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscribedObservers_WhenAnExistingObserverResubscribes_ThenDoNotNotifyTheOtherObservers);
        var clientOne = "client-1";
        var firstSubscriber = Substitute.For<IChatObserver>();
        var secondSubscriber = Substitute.For<IChatObserver>();
        var grain = await _fixture
            .GetGrainBuilder()
            .SetGrain(grainId)
            .WithSubscriber(clientOne, firstSubscriber)
            .WithSubscriber("client-2", secondSubscriber)
            .Build();

        // Act
        await grain.Subscribe(clientOne, _fixture.Cluster.GrainFactory.CreateObjectReference<IChatObserver>(firstSubscriber));

        // Assert
        await secondSubscriber.DidNotReceiveWithAnyArgs().ReceiveMessage(Arg.Any<IMessage>());
    }

    [Fact]
    public async Task GivenMultipleSubscriptions_WhenAnExistingSubscriptionIsReregisteredWithANewObserverAndAMessageIsSent_ThenSendTheMessageToTheNewObserverInsteadOfTheOldOne()
    {
        // Arrange
        var grainId = nameof(GivenMultipleSubscriptions_WhenAnExistingSubscriptionIsReregisteredWithANewObserverAndAMessageIsSent_ThenSendTheMessageToTheNewObserverInsteadOfTheOldOne);
        var clientOne = "client-1";
        var clientTwo = "client-2";
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
        await grain.Subscribe(clientOne, _fixture.Cluster.GrainFactory.CreateObjectReference<IChatObserver>(thirdObserver));
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

    // GivenMultipleSubscribedObservers_WhenOneObserverUnsubscribes_ThenNotifyTheRemainingObserer

    // GivenAnObserverThatHasUnsubscribed_WhenAMessageIsSentToTheGrain_ThenTheGrainDoesNotAttemptToDeliverAnyMoreMessagesToTheUnsubscribedObserver

    // GivenMultipleSubscribedObservers_WhenAMessageIsSentAndOneObserverFails_ThenTheGrainDoesNotAttemptToDeliverAnyMoreMessagesToTheFailedObserver
}

public class TestClusterFixture : IDisposable
{
    public TestClusterFixture() => Cluster.Deploy();

    public TestCluster Cluster { get; } = new TestClusterBuilder()
        .AddSiloBuilderConfigurator<TestSiloConfigurations>()
        .AddClientBuilderConfigurator<TestClientConfigurations>()
        .Build();

    void IDisposable.Dispose() => Cluster.StopAllSilos();

    public GrainBuilder GetGrainBuilder() => new(Cluster);

    private class TestSiloConfigurations : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.Services.AddSerializer(serializerBuilder =>
            {
                serializerBuilder.AddJsonSerializer(
                    isSupported: type => type.Namespace!.StartsWith("Shared"));
            });
        }
    }

    private class TestClientConfigurations : IClientBuilderConfigurator
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

public class GrainBuilder
{
    private readonly TestCluster _cluster;
    private readonly ICollection<(string Id, IChatObserver Observer)> _observers = new List<(string Id, IChatObserver Observer)>();
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

    public GrainBuilder WithSubscriber(string observerId, IChatObserver observer)
    {
        _observers.Add((observerId, observer));

        return this;
    }
}
