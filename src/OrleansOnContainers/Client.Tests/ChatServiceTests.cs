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
    public async Task WhenAClientSendsAMessage_ForwardTheMessageToTheChatGrain()
    {
        // Arrange
        var clusterClient = Substitute.For<IClusterClient>();
        var grain = Substitute.For<IChatGrain>();
        clusterClient
            .GetGrain<IChatGrain>(Arg.Any<long>())
            .ReturnsForAnyArgs(grain);
        var service = new ChatService(clusterClient, new NullLogger<ChatService>());
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
        var service = new ChatService(Substitute.For<IClusterClient>(), new NullLogger<ChatService>());
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

        // Act

        // Assert
        throw new NotImplementedException();
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
