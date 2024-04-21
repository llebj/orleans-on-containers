namespace Client.Application.Contracts;

public interface IMessageStreamInput
{
    ChannelStatus InputStatus { get; }

    (MessageStreamWriter Writer, Guid ReleaseKey) GetWriter();

    void ReleaseWriter(Guid releaseKey);
}
