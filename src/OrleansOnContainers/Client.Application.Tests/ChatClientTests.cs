using GrainInterfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Client.Application.Tests;

public class ChatClientTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GivenAClientWithAProposedScreenName_WhenTheClientJoinsAChat_ThenReturnAResultCorrespondingToTheScreenNameAvailability(bool availability)
    {
        // Arrange
        var chat = "test";
        var screenName = "test";
        var grainFactory = Substitute.For<IGrainFactory>();
        var grain = Substitute.For<IChatGrain>();
        grain.ScreenNameIsAvailable(screenName).Returns(availability);
        grainFactory.GetGrain<IChatGrain>(chat).Returns(grain);
        var client = new ChatClient(grainFactory, new NullLogger<ChatClient>());

        // Act
        var result = await client.JoinChat(chat, Guid.NewGuid(), screenName);

        // Assert
        Assert.Equal(availability, result.IsSuccess);
    }

    [Fact]
    public async Task GivenAClientThatHasJoinedAChat_WhenTheClientJoinsAChatAgain_ThenReturnAFailureResult()
    {
        // Arrange
        var chat = "test";
        var screenName = "test";
        var grainFactory = Substitute.For<IGrainFactory>();
        var grain = Substitute.For<IChatGrain>();
        grain.ScreenNameIsAvailable(screenName).Returns(true);
        grainFactory.GetGrain<IChatGrain>(chat).Returns(grain);
        var client = new ChatClient(grainFactory, new NullLogger<ChatClient>());
        await client.JoinChat(chat, Guid.NewGuid(), screenName);

        // Act
        var result = await client.JoinChat(chat, Guid.NewGuid(), screenName);

        // Assert
        Assert.False(result.IsSuccess);
    }
}
