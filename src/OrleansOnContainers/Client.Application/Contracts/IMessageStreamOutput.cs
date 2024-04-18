namespace Client.Application.Contracts;

public interface IMessageStreamOutput
{
    bool ReaderIsAllocated { get; }

    (MessageStreamReader Reader, Guid ReleaseKey) GetReader();

    void ReleaseReader(Guid releaseKey);
}
