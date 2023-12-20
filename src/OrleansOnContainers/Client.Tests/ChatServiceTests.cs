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
    public async Task GivenAnActiveSubscription_WhenAClientSendsAMessage_ThenRetrieveTheCorrectChatGrain()
    {
        // Arrange
        var chat = "chat";
        var clusterClient = Substitute.For<IClusterClient>();
        var observerManager = Substitute.For<IGrainObserverManager>();
        observerManager.Subscribe(Arg.Any<IChatObserver>(), chat).Returns(Result.Success());
        var service = new ChatService(clusterClient, observerManager, new NullLogger<ChatService>());
        await service.Join(chat, _fixture.ClientId);

        // Act
        await service.SendMessage(_fixture.ClientId, "message");

        // Assert
        clusterClient.Received().GetGrain<IChatGrain>(chat);
    }

    [Fact]
    public async Task GivenAnActiveSubscription_WhenAClientSendsAMessage_ThenForwardTheMessageToTheChatGrain()
    {
        // Arrange
        var chat = "chat";
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        clusterClient
            .GetGrain<IChatGrain>(chat)
            .Returns(grain);
        var observerManager = Substitute.For<IGrainObserverManager>();
        observerManager.Subscribe(Arg.Any<IChatObserver>(), chat).Returns(Result.Success());
        var service = new ChatService(clusterClient, observerManager, new NullLogger<ChatService>());
        await service.Join(chat, _fixture.ClientId);
        var message = "test";

        // Act
        await service.SendMessage(_fixture.ClientId, message);

        // Assert
        await grain.Received().SendMessage(_fixture.ClientId, message);
    }

    [Fact] 
    public async Task GivenAnActiveSubscription_WhenAClientSendsAMessageAndTheMessageIsSent_ThenReturnASuccessResult()
    {
        // Arrange
        var chat = "chat";
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        clusterClient
            .GetGrain<IChatGrain>(chat)
            .Returns(grain);
        var observerManager = Substitute.For<IGrainObserverManager>();
        observerManager.Subscribe(Arg.Any<IChatObserver>(), chat).Returns(Result.Success());
        var service = new ChatService(clusterClient, observerManager, new NullLogger<ChatService>());
        await service.Join(chat, _fixture.ClientId);
        var message = "test";

        // Act
        var result = await service.SendMessage(_fixture.ClientId, message);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GivenNoActiveSubscription_WhenAClientSendsAMessage_ThenReturnAFailureResultWithAMessage()
    {
        // Arrange
        var chat = "chat";
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        clusterClient
            .GetGrain<IChatGrain>(chat)
            .Returns(grain);
        var service = new ChatService(clusterClient, Substitute.For<IGrainObserverManager>(), new NullLogger<ChatService>());
        var message = "test";

        // Act
        var result = await service.SendMessage(_fixture.ClientId, message);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(message));
    }

    [Fact]
    public async Task GivenAnActiveSubscription_WhenAClientSendsAMessageAndCallingTheGrainFails_ThenReturnAFailureResultWithAMessage()
    {
        // Arrange
        var chat = "chat";
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        grain
            .SendMessage(_fixture.ClientId, Arg.Any<string>())
            .Returns(x => { throw new Exception(); });
        clusterClient
            .GetGrain<IChatGrain>(chat)
            .Returns(grain);
        var service = new ChatService(clusterClient, Substitute.For<IGrainObserverManager>(), new NullLogger<ChatService>());
        await service.Join(chat, _fixture.ClientId);
        var message = "test";

        // Act
        var result = await service.SendMessage(_fixture.ClientId, message);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(message));
    }

    [Fact]
    public async Task WhenTheServiceReceivesAMessage_ThenInvokeTheMessageReceivedEventHandler()
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
    public async Task WhenAClientRequestsToJoinAChat_ThenSubscribeToChatMessages()
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
    public async Task WhenAClientRequestsToJoinAChatAndTheRequestSucceeds_ThenReturnASuccessResult()
    {
        // Arrange
        var chat = "test";
        var clusterClient = Substitute.For<IClusterClient>();
        var observerManager = Substitute.For<IGrainObserverManager>();
        observerManager.Subscribe(Arg.Any<IChatObserver>(), chat).Returns(Result.Success());
        var service = new ChatService(clusterClient, observerManager, new NullLogger<ChatService>());

        // Act
        var result = await service.Join(chat, _fixture.ClientId);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task WhenAClientRequestsToJoinAChatAndTheManagerThrowsAnException_ThenReturnAFailureResultWithAMessage()
    {
        // Arrange
        var chat = "test";
        var message = "error";
        var clusterClient = Substitute.For<IClusterClient>();
        var observerManager = Substitute.For<IGrainObserverManager>();
        observerManager.Subscribe(Arg.Any<IChatObserver>(), chat).Returns<Task<Result>>(x => { throw new Exception(message); });
        var service = new ChatService(clusterClient, observerManager, new NullLogger<ChatService>());

        // Act
        var result = await service.Join(chat, _fixture.ClientId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.Message));
    }

    [Fact]
    public async Task WhenAClientRequestsToJoinAChatAndTheManagerReturnsAFailureResult_ThenReturnAFailureResultWithAMessage()
    {
        // Arrange
        var chat = "test";
        var clusterClient = Substitute.For<IClusterClient>();
        var observerManager = Substitute.For<IGrainObserverManager>();
        observerManager.Subscribe(Arg.Any<IChatObserver>(), chat).Returns(Result.Failure("error"));
        var service = new ChatService(clusterClient, observerManager, new NullLogger<ChatService>());

        // Act
        var result = await service.Join(chat, _fixture.ClientId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.Message));
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
