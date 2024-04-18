namespace Client.Application.Contracts;

public interface IMessageStreamInput
{
    bool WriterIsAllocated { get; }

    (MessageStreamWriter Writer, Guid ReleaseKey) GetWriter();

    void ReleaseWriter(Guid releaseKey);
}
