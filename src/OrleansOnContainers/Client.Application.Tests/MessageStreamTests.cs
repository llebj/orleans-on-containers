using GrainInterfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Client.Application.Tests;

public class MessageStreamTests
{
    public class OutputTests
    {
        private readonly ILogger<MessageStream> _logger = new NullLogger<MessageStream>();

        [Fact]
        public void GivenAnOpenOutputChannel_WhenTheReaderIsRetreived_ThenMarkTheOutputAsAllocated()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);

            // Act
            _ = messageStream.GetReader();

            // Assert
            Assert.Equal(ChannelStatus.Allocated, messageStream.OutputStatus);
        }

        [Fact]
        public void GivenAnOpenOutputChannel_WhenTheReaderIsRetreived_ThenReturnNonEmptyValues()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);

            // Act
            var (Reader, ReleaseKey) = messageStream.GetReader();

            // Assert
            Assert.NotNull(Reader);
            Assert.NotEqual(Guid.Empty, ReleaseKey);
        }

        [Fact]
        public void GivenAnOpenOutputChannel_WhenTheReaderIsRelesed_ThenThrowAnInvalidOperationException()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => messageStream.ReleaseReader(Guid.NewGuid()));
        }

        [Fact]
        public void GivenAnAllocatedOutputChannel_WhenTheReaderIsRetreived_ThenThrowAnInvalidOperationException()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);
            _ = messageStream.GetReader();

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => messageStream.GetReader());
        }

        [Fact]
        public void GivenAnAllocatedOutputChannel_WhenTheReaderIsReleasedWithTheCorrectKey_ThenMarkTheOutputAsAwaitingCompletion()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);
            var (_, ReleaseKey) = messageStream.GetReader();

            // Act
            messageStream.ReleaseReader(ReleaseKey);

            // Assert
            Assert.Equal(ChannelStatus.AwaitingCompletion, messageStream.OutputStatus);
        }

        [Fact]
        public void GivenAnAllocatedOutputChannel_WhenTheReaderIsReleasedWithTheIncorrectKey_ThenThrowAnInvalidOperationException()
        {
            // Arrange
            var testKey = Guid.NewGuid();
            var messageStream = new MessageStream(_logger);
            var (_, ReleaseKey) = messageStream.GetReader();

            // Ew...
            while (testKey == ReleaseKey)
            {
                testKey = Guid.NewGuid();
            }

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => messageStream.ReleaseReader(testKey));
        }

        [Fact]
        public void GivenAnAwaitingCompletionOutputChannel_WhenMessagesAreReadFromTheReader_ThenThrowAnInvalidOperationException()
        {
            // Arrange
            var message = new FakeMessage();
            var messageStream = new MessageStream(_logger);
            var (Reader, ReleaseKey) = messageStream.GetReader();
            messageStream.ReleaseReader(ReleaseKey);

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(Reader.ReadMessages);
        }

        [Fact]
        public void GivenAnAwaitingCompletionOutputChannel_WhenAReaderIsRetrieved_ThenThrowAnInvalidOperationException()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);
            var (_, ReleaseKey) = messageStream.GetReader();
            messageStream.ReleaseReader(ReleaseKey);

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => messageStream.GetReader());
        }

        [Fact]
        public void GivenAnAwaitingCompletionOutputChannel_WhenAReaderIsReleased_ThenThrowAnInvalidOperationException()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);
            var (_, ReleaseKey) = messageStream.GetReader();
            messageStream.ReleaseReader(ReleaseKey);

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => messageStream.ReleaseReader(ReleaseKey));
        }
    }

    public class InputTests
    {
        private readonly ILogger<MessageStream> _logger = new NullLogger<MessageStream>();

        [Fact]
        public void GivenAnOpenInputChannel_WhenTheWriterIsRetreived_ThenMarkTheInputAsBeingAllocated()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);

            // Act
            _ = messageStream.GetWriter();

            // Assert
            Assert.Equal(ChannelStatus.Allocated, messageStream.InputStatus);
        }

        [Fact]
        public void GivenAnOpenInputChannel_WhenTheWriterIsRetreived_ThenReturnNonEmptyValues()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);

            // Act
            var (Writer, ReleaseKey) = messageStream.GetWriter();

            // Assert
            Assert.NotNull(Writer);
            Assert.NotEqual(default, ReleaseKey);
        }

        [Fact]
        public void GivenAnOpenInputChannel_WhenTheWriterIsRelesed_ThenThrowAnInvalidOperationException()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => messageStream.ReleaseWriter(Guid.NewGuid()));
        }

        [Fact]
        public void GivenAnAllocatedInputChannel_WhenTheWriterIsRetreived_ThenThrowAnInvalidOperationException()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);
            _ = messageStream.GetWriter();

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => messageStream.GetWriter());
        }

        [Fact]
        public void GivenAnAllocatedInputChannel_WhenTheWriterIsReleasedWithTheCorrectKey_ThenMarkTheInputAsAwaitingCompletion()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);
            var (_, ReleaseKey) = messageStream.GetWriter();

            // Act
            messageStream.ReleaseWriter(ReleaseKey);

            // Assert
            Assert.Equal(ChannelStatus.AwaitingCompletion, messageStream.InputStatus);
        }

        [Fact]
        public void GivenAnAllocatedInputChannel_WhenTheWriterIsReleasedWithTheIncorrectKey_ThenThrowAnInvalidOperationException()
        {
            // Arrange
            var testKey = Guid.NewGuid();
            var messageStream = new MessageStream(_logger);
            var (_, ReleaseKey) = messageStream.GetWriter();

            // Ew...
            while (testKey == ReleaseKey)
            {
                testKey = Guid.NewGuid();
            }

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => messageStream.ReleaseWriter(testKey));
        }

        [Fact]
        public async Task GivenAwaitingCompletionInputChannel_WhenAMessageIsWrittenToTheWriter_ThenThrowAnInvalidOperationException()
        {
            // Arrange
            var message = new FakeMessage();
            var messageStream = new MessageStream(_logger);
            var (Writer, ReleaseKey) = messageStream.GetWriter();
            messageStream.ReleaseWriter(ReleaseKey);

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => Writer.WriteMessage(message, CancellationToken.None));
        }

        [Fact]
        public void GivenAwaitingCompletionInputChannel_WhenAWriterIsRetrieved_ThenThrowAnInvalidOperationException()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);
            var (_, ReleaseKey) = messageStream.GetWriter();
            messageStream.ReleaseWriter(ReleaseKey);

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => messageStream.GetWriter());
        }

        [Fact]
        public void GivenAwaitingCompletionInputChannel_WhenAWriterIsReleased_ThenThrowAnInvalidOperationException()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);
            var (_, ReleaseKey) = messageStream.GetWriter();
            messageStream.ReleaseWriter(ReleaseKey);

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => messageStream.ReleaseWriter(ReleaseKey));
        }
    }

    public class ChannelCompletionTests
    {
        private readonly ILogger<MessageStream> _logger = new NullLogger<MessageStream>();

        [Fact]
        public async Task GivenAnInputChannelAwaitingCompletionAndAnAllocatedOutputChannel_WhenMessagesAreRead_ThenReadsToTheEndOfTheStream()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);
            var (_, WriterKey) = messageStream.GetWriter();
            var (Reader, ReaderKey) = messageStream.GetReader();
            messageStream.ReleaseWriter(WriterKey);

            // Act
            var enumerator = Reader.ReadMessages().GetAsyncEnumerator();

            // Assert
            Assert.False(await enumerator.MoveNextAsync());
        }

        [Fact]
        public void GivenAnOutputChannelAwaitingCompletionAndAnAllocatedInputChannel_WhenTheInputChannelIsReleased_ThenBothChannelsRevertToOpen()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);
            var (_, WriterKey) = messageStream.GetWriter();
            var (_, ReaderKey) = messageStream.GetReader();
            messageStream.ReleaseWriter(WriterKey);

            // Act
            messageStream.ReleaseReader(ReaderKey);

            // Assert
            Assert.Equal(ChannelStatus.Open, messageStream.InputStatus);
            Assert.Equal(ChannelStatus.Open, messageStream.OutputStatus);
        }

        [Fact]
        public void GivenAnInputChannelAwaitingCompletionAndAnAllocatedOutputChannel_WhenTheOutputChannelIsReleased_ThenBothChannelsRevertToOpen()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);
            var (_, WriterKey) = messageStream.GetWriter();
            var (_, ReaderKey) = messageStream.GetReader();
            messageStream.ReleaseReader(ReaderKey);

            // Act
            messageStream.ReleaseWriter(WriterKey);

            // Assert
            Assert.Equal(ChannelStatus.Open, messageStream.InputStatus);
            Assert.Equal(ChannelStatus.Open, messageStream.OutputStatus);
        }

        [Fact(Skip = "This test never completes. Not sure if it is possible to test this piece of functionality.")]
        public async Task GivenAnOutputChannelThatIsAwaitingCompletionAndAnInputChannelThatWritesAMessageBeforeBeingReleased_WhenANewReaderIsObtainedAndMessagesAreRead_ThenReadToTheEndOfTheStream()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);
            var (Writer, WriterKey) = messageStream.GetWriter();
            var (_, ReaderKey) = messageStream.GetReader();
            messageStream.ReleaseReader(ReaderKey);

            var message = new FakeMessage();
            await Writer.WriteMessage(message, CancellationToken.None);
            messageStream.ReleaseWriter(WriterKey);

            // Act
            var (Reader, _) = messageStream.GetReader();
            var enumerator = Reader.ReadMessages().GetAsyncEnumerator();

            // Assert
            Assert.False(await enumerator.MoveNextAsync());
        }
    }
}

internal class FakeMessage : IMessage
{
    public MessageCategory Category => MessageCategory.User;

    public string Chat => string.Empty;

    public string Message => string.Empty;

    public string Sender => string.Empty;

    public DateTimeOffset SentAt => DateTimeOffset.UtcNow;
}
