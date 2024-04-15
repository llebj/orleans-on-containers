using Client.Application.Contracts;
using GrainInterfaces;
using NSubstitute;
using Xunit;

namespace Client.Application.Tests;

public class ChatAccessClientTests
{
    private readonly Guid _clientId = Guid.NewGuid();

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
        var client = new ChatAccessClient(grainFactory, Substitute.For<IMessageStreamWriterAllocator>());

        // Act
        var result = await client.JoinChat(chat, _clientId, screenName);

        // Assert
        Assert.Equal(availability, result.IsSuccess);
    }
}
