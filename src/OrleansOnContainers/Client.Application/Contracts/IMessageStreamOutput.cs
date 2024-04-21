namespace Client.Application.Contracts;

public interface IMessageStreamOutput
{
    ChannelStatus OutputStatus { get; }

    (MessageStreamReader Reader, Guid ReleaseKey) GetReader();

    void ReleaseReader(Guid releaseKey);
}
