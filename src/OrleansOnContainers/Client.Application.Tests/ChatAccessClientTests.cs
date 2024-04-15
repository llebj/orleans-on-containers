using Client.Application.Contracts;
using Client.Application.Options;
using GrainInterfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Client.Application.Tests;

public class ChatAccessClientTests
{
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly IOptions<ResubscriberOptions> _options;
    private readonly TimeProvider _timeProvider = new FakeTimeProvider();

    public ChatAccessClientTests()
    {
        _options = Substitute.For<IOptions<ResubscriberOptions>>();
        _options.Value.Returns(new ResubscriberOptions { RefreshPeriod = 100 });
    }

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
        var client = new ChatAccessClient(grainFactory, Substitute.For<IMessageStreamWriterAllocator>(), _options, _timeProvider);

        // Act
        var result = await client.JoinChat(chat, _clientId, screenName);

        // Assert
        Assert.Equal(availability, result.IsSuccess);
    }
}
