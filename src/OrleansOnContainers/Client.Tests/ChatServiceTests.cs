using Client.Options;
using Client.Services;
using GrainInterfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Client.Tests;

public class ChatServiceTests : IClassFixture<ChatServiceTestsFixture>
{
    private readonly ChatServiceTestsFixture _fixture;

    public ChatServiceTests(
        ChatServiceTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GivenAnActiveSubscription_WhenAClientSendsAMessage_RetrieveTheCorrectChatGrain()
    {
        // Arrange
        var chat = "chat";
        var clusterClient = Substitute.For<IClusterClient>();
        var service = new ChatService(clusterClient, Substitute.For<IGrainObserverManager>(), new NullLogger<ChatService>());
        await service.Join(chat, _fixture.ClientId);

        // Act
        await service.SendMessage(_fixture.ClientId, "message");

        // Assert
        clusterClient.Received().GetGrain<IChatGrain>(chat);
    }

    [Fact]
    public async Task GivenAnActiveSubscription_WhenAClientSendsAMessage_ForwardTheMessageToTheChatGrain()
    {
        // Arrange
        var chat = "chat";
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        clusterClient
            .GetGrain<IChatGrain>(chat)
            .Returns(grain);
        var service = new ChatService(clusterClient, Substitute.For<IGrainObserverManager>(), new NullLogger<ChatService>());
        await service.Join(chat, _fixture.ClientId);
        var message = "test";

        // Act
        await service.SendMessage(_fixture.ClientId, message);

        // Assert
        await grain.Received().SendMessage(_fixture.ClientId, message);
    }

    [Fact]
    public async Task WhenTheServiceReceivesAMessage_InvokeTheMessageReceivedEventHandler()
    {
        // Arrange
        var service = new ChatService(Substitute.For<IClusterClient>(), Substitute.For<IGrainObserverManager>(), new NullLogger<ChatService>());
        var handler = Substitute.For<EventHandler<MessageReceivedEventArgs>>();
        service.MessageReceived += handler;
        var message = "test";

        // Act
        await service.ReceiveMessage(_fixture.ClientId, message);

        // Assert
        handler
            .Received()
            .Invoke(
                service, 
                Arg.Is<MessageReceivedEventArgs>(x => 
                    x.Clientid == _fixture.ClientId && 
                    x.Message == message));
    }

    [Fact]
    public async Task WhenAClientRequestsToJoinAChat_SubscribeToChatMessages()
    {
        // Arrange
        var chat = "test";
        var clusterClient = Substitute.For<IClusterClient>();
        var observerManager = Substitute.For<IGrainObserverManager>();
        var service = new ChatService(clusterClient, observerManager, new NullLogger<ChatService>());

        // Act
        await service.Join(chat, _fixture.ClientId);

        // Assert
        await observerManager.Received().Subscribe(service, chat);
    }

    [Fact]
    public async Task WhenAClientRequestsToJoinAChatAndTheRequestSucceeds_ThenReturnATrueSuccessResult()
    {
        // Arrange
        var chat = "test";
        var clusterClient = Substitute.For<IClusterClient>();
        var observerManager = Substitute.For<IGrainObserverManager>();
        var service = new ChatService(clusterClient, observerManager, new NullLogger<ChatService>());

        // Act
        var result = await service.Join(chat, _fixture.ClientId);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task WhenAClientRequestsToJoinAChatAndTheRequestFails_ThenReturnAFalseSuccessResultWithAMessage()
    {
        // Arrange
        var chat = "test";
        var message = "error";
        var clusterClient = Substitute.For<IClusterClient>();
        var observerManager = Substitute.For<IGrainObserverManager>();
        observerManager.Subscribe(Arg.Any<IChatObserver>(), chat).Returns(x => { throw new Exception(message); });
        var service = new ChatService(clusterClient, observerManager, new NullLogger<ChatService>());

        // Act
        var result = await service.Join(chat, _fixture.ClientId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(message, result.Message);
    }
}

public class ChatServiceTestsFixture
{
    public ChatServiceTestsFixture()
    {
        ClientOptions = MockOptions();
    }

    public Guid ClientId { get; } = Guid.Parse("3b7a1546-a832-4b03-a0b1-f0a2c629a30f");

    public IOptions<ClientOptions> ClientOptions { get; private set; }

    private IOptions<ClientOptions> MockOptions()
    {
        var substitute = Substitute.For<IOptions<ClientOptions>>();
        substitute.Value.Returns(new ClientOptions(ClientId));

        return substitute;
    }
}
