using Client.Services;
using GrainInterfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Client.Tests;

public class ChatServiceTests
{
    public class JoinTests
    {
        private readonly Guid _clientId = Guid.Parse("3b7a1546-a832-4b03-a0b1-f0a2c629a30f");

        [Fact]
        public async Task WhenAClientRequestsToJoinAChatAndTheRequestSucceeds_ThenReturnASuccessResult()
        {
            // Arrange
            var chat = "test";
            var clusterClient = Substitute.For<IClusterClient>();
            var subscriptionManager = Substitute.For<ISubscriptionManager>();
            subscriptionManager.Subscribe(chat).Returns(Result.Success());
            var service = new ChatService(clusterClient, subscriptionManager, new NullLogger<ChatService>());

            // Act
            var result = await service.Join(chat, _clientId);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task WhenAClientRequestsToJoinAChatAndTheManagerReturnsAFailureResult_ThenReturnAFailureResult()
        {
            // Arrange
            var chat = "test";
            var clusterClient = Substitute.For<IClusterClient>();
            var subscriptionManager = Substitute.For<ISubscriptionManager>();
            subscriptionManager.Subscribe(chat).Returns(Result.Failure("error"));
            var service = new ChatService(clusterClient, subscriptionManager, new NullLogger<ChatService>());

            // Act
            var result = await service.Join(chat, _clientId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.False(string.IsNullOrEmpty(result.Message));
        }
    }

    public class SendMessageTests
    {
        private readonly string _clientId = "client";

        [Theory]
        [InlineData("test")]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GivenAnActiveSubscription_WhenAClientSendsAMessage_ThenReturnASuccessResult(string message)
        {
            // Arrange
            var chat = "chat";
            var clusterClient = Substitute.For<IClusterClient>();
            var grain = Substitute.For<IChatGrain>();
            clusterClient
                .GetGrain<IChatGrain>(chat)
                .Returns(grain);
            var subscriptionManager = Substitute.For<ISubscriptionManager>();
            subscriptionManager.Subscribe(chat).Returns(Result.Success());
            var service = new ChatService(clusterClient, subscriptionManager, new NullLogger<ChatService>());
            await service.Join(chat, default);

            // Act
            var result = await service.SendMessage(_clientId, message);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GivenNoActiveSubscription_WhenAClientSendsAMessage_ThenReturnAFailureResult()
        {
            // Arrange
            var chat = "chat";
            var clusterClient = Substitute.For<IClusterClient>();
            var grain = Substitute.For<IChatGrain>();
            clusterClient
                .GetGrain<IChatGrain>(chat)
                .Returns(grain);
            var service = new ChatService(clusterClient, Substitute.For<ISubscriptionManager>(), new NullLogger<ChatService>());
            var message = "test";

            // Act
            var result = await service.SendMessage(_clientId, message);

            // Assert
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task GivenAnActiveSubscription_WhenAClientSendsAMessageAndCallingTheGrainFails_ThenReturnAFailureResult()
        {
            // Arrange
            var chat = "chat";
            var clusterClient = Substitute.For<IClusterClient>();
            var grain = Substitute.For<IChatGrain>();
            grain
                .SendMessage(_clientId, Arg.Any<string>())
                .Returns(x => { throw new Exception(); });
            clusterClient
                .GetGrain<IChatGrain>(chat)
                .Returns(grain);
            var subscriptionManager = Substitute.For<ISubscriptionManager>();
            subscriptionManager.Subscribe(chat).Returns(Result.Success());
            var service = new ChatService(clusterClient, subscriptionManager, new NullLogger<ChatService>());
            await service.Join(chat, default);
            var message = "test";

            // Act
            var result = await service.SendMessage(_clientId, message);

            // Assert
            Assert.False(result.IsSuccess);
        }
    }
}
