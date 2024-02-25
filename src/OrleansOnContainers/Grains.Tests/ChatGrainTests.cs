using GrainInterfaces;
using Microsoft.Extensions.Configuration;
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
    public async Task GivenNoSubscribers_WhenAMessageIsSent_ThenDoNotThrowAnException()
    {
        // Arrange
        var grain = _cluster.GrainFactory.GetGrain<IChatGrain>("test");

        // Act
        await grain.SendMessage("client", "hello");

        // Assert
    }

    [Fact]
    public async Task GivenMultipleSubscribedObservers_WhenAMessageIsSentToTheGrain_ThenAllTheObserversReceiveTheMessage()
    {
        // Arrange
        var grain = _cluster.GrainFactory.GetGrain<IChatGrain>("test");
        var primarySubscriber = Substitute.For<IChatObserver>();
        var secondarySubscriber = Substitute.For<IChatObserver>();
        await grain.Subscribe(_cluster.GrainFactory.CreateObjectReference<IChatObserver>(primarySubscriber));
        await grain.Subscribe(_cluster.GrainFactory.CreateObjectReference<IChatObserver>(secondarySubscriber));

        // Act
        await grain.SendMessage("client", "hello");

        // Assert
        await primarySubscriber.Received().ReceiveMessage(Arg.Any<ChatMessage>());
        await secondarySubscriber.Received().ReceiveMessage(Arg.Any<ChatMessage>());
    }

    // GivenASubscribedObserver_WhenAnotherObserverSubscribes_ThenNotifyTheExistingObserver

    // GivenMultipleSubscribedObservers_WhenOneObserverUnsubscribes_ThenNotifyTheRemainingObserer

    // GivenAnObserverThatHasUnsubscribed_WhenAMessageIsSentToTheGrain_ThenTheObserverDoesNotReceiveAnyMoreMessages
}

public class TestClusterFixture : IDisposable
{
    public TestClusterFixture() => Cluster.Deploy();

    public TestCluster Cluster { get; } = new TestClusterBuilder()
        .AddSiloBuilderConfigurator<TestSiloConfigurations>()
        .AddClientBuilderConfigurator<TestClientConfigurations>()
        .Build();

    void IDisposable.Dispose() => Cluster.StopAllSilos();

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
