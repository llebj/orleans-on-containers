namespace Client.Application.Contracts;

public interface IMessageStreamReaderAllocator
{
    bool ReaderIsAllocated { get; }

    (MessageStreamReader Reader, Guid ReleaseKey) GetReader();

    void ReleaseReader(Guid releaseKey);
}
