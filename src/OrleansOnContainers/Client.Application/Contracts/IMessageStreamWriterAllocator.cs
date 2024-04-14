namespace Client.Application.Contracts;

public interface IMessageStreamWriterAllocator
{
    bool WriterIsAllocated { get; }

    (MessageStreamWriter Writer, Guid ReleaseKey) GetWriter();

    void ReleaseWriter(Guid releaseKey);
}
