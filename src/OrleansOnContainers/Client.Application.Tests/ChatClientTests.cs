using Client.Application.Contracts;
using GrainInterfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Client.Application.Tests;

public class ChatClientTests
{
    private readonly NullLogger<ChatClient> _logger = new();
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
        var client = new ChatClient(grainFactory, _logger, Substitute.For<IMessageStream>());

        // Act
        var result = await client.JoinChat(chat, _clientId, screenName);

        // Assert
        Assert.Equal(availability, result.IsSuccess);
    }
}
