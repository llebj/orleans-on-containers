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

    // GivenAnyObservers_WhenAMessageIsSentFromAnUnsubscribedClient_ThenReturnAnUnsuccessfulResult

    [Fact]
    public async Task GivenMultipleSubscribedObservers_WhenAMessageIsSentToTheGrain_ThenAllTheObserversReceiveTheMessage()
    {
        // Arrange
        var grainId = "test";
        var clientId = "client";
        var message = "hello";
        var grain = _cluster.GrainFactory.GetGrain<IChatGrain>(grainId);
        var primarySubscriber = Substitute.For<IChatObserver>();
        var secondarySubscriber = Substitute.For<IChatObserver>();
        await grain.Subscribe(_cluster.GrainFactory.CreateObjectReference<IChatObserver>(primarySubscriber));
        await grain.Subscribe(_cluster.GrainFactory.CreateObjectReference<IChatObserver>(secondarySubscriber));

        // Act
        await grain.SendMessage(clientId, message);

        // Assert
        await primarySubscriber.Received().ReceiveMessage(
            Arg.Is<IMessage>(m => 
                m.Category == MessageCategory.User && 
                m.Chat == grainId && 
                m.ClientId == clientId && 
                m.Message == message));
        await secondarySubscriber.Received().ReceiveMessage(
            Arg.Is<IMessage>(m =>
                m.Category == MessageCategory.User &&
                m.Chat == grainId &&
                m.ClientId == clientId &&
                m.Message == message));
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
