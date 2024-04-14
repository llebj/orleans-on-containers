using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Messages;
using Xunit;

namespace Client.Application.Tests;

public class MessageStreamTests
{
    public class ReaderTests
    {
        private readonly ILogger<MessageStream> _logger = new NullLogger<MessageStream>();

        [Fact]
        public void GivenNoAllocatedReader_WhenTheReaderIsRetreived_ThenMarkTheReaderAsBeingAllocated()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);

            // Act
            _ = messageStream.GetReader();

            // Assert
            Assert.True(messageStream.ReaderIsAllocated);
        }

        [Fact]
        public void GivenNoAllocatedReader_WhenTheReaderIsRelesed_ThenMarkTheReaderAsNotBeingAllocated()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);

            // Act
            messageStream.ReleaseReader(Guid.NewGuid());

            // Assert
            Assert.False(messageStream.ReaderIsAllocated);
        }

        [Fact]
        public void GivenAnAllocatedReader_WhenTheReaderIsRetreived_ThenThrowAnInvalidOperationException()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);
            _ = messageStream.GetReader();

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => messageStream.GetReader());
        }

        [Fact]
        public void GivenAnAllocatedReader_WhenTheReaderIsReleasedWithTheCorrectKey_ThenMarkTheReaderAsNotBeingAllocated()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);
            var (_, ReleaseKey) = messageStream.GetReader();

            // Act
            messageStream.ReleaseReader(ReleaseKey);

            // Assert
            Assert.False(messageStream.ReaderIsAllocated);
        }

        [Fact]
        public void GivenAnAllocatedReader_WhenTheReaderIsReleasedWithTheIncorrectKey_ThenThrowAnInvalidOperationException()
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
        public void GivenAnUnallocatedReader_WhenMessagesAreReadFromTheReader_ThenThrowAnInvalidOperationException()
        {
            // Arrange
            var message = new ChatMessage("test", "test", "test");
            var messageStream = new MessageStream(_logger);
            var (Reader, ReleaseKey) = messageStream.GetReader();
            messageStream.ReleaseReader(ReleaseKey);

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(Reader.ReadMessages);
        }
    }

    public class WriterTests
    {
        private readonly ILogger<MessageStream> _logger = new NullLogger<MessageStream>();

        [Fact]
        public void GivenNoAllocatedWriter_WhenTheWriterIsRetreived_ThenMarkTheWriterAsBeingAllocated()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);

            // Act
            _ = messageStream.GetWriter();

            // Assert
            Assert.True(messageStream.WriterIsAllocated);
        }

        [Fact]
        public void GivenNoAllocatedWriter_WhenTheWriterIsRelesed_ThenMarkTheWriterAsNotBeingAllocated()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);

            // Act
            messageStream.ReleaseWriter(Guid.NewGuid());

            // Assert
            Assert.False(messageStream.WriterIsAllocated);
        }

        [Fact]
        public void GivenAnAllocatedWriter_WhenTheWriterIsRetreived_ThenThrowAnInvalidOperationException()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);
            _ = messageStream.GetWriter();

            // Act
            // Assert
            Assert.Throws<InvalidOperationException>(() => messageStream.GetWriter());
        }

        [Fact]
        public void GivenAnAllocatedWriter_WhenTheWriterIsReleasedWithTheCorrectKey_ThenMarkTheWriterAsNotBeingAllocated()
        {
            // Arrange
            var messageStream = new MessageStream(_logger);
            var (_, ReleaseKey) = messageStream.GetWriter();

            // Act
            messageStream.ReleaseWriter(ReleaseKey);

            // Assert
            Assert.False(messageStream.WriterIsAllocated);
        }

        [Fact]
        public void GivenAnAllocatedWriter_WhenTheWriterIsReleasedWithTheIncorrectKey_ThenThrowAnInvalidOperationException()
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
        public async Task GivenAnUnallocatedWriter_WhenAMessageIsWrittenToTheWriter_ThenThrowAnInvalidOperationException()
        {
            // Arrange
            var message = new ChatMessage("test", "test", "test");
            var messageStream = new MessageStream(_logger);
            var (Writer, ReleaseKey) = messageStream.GetWriter();
            messageStream.ReleaseWriter(ReleaseKey);

            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => Writer.WriteMessage(message, CancellationToken.None));
        }
    }
}
