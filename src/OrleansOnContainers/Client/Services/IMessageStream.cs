using Shared.Messages;

namespace Client.Services;

internal interface IMessageStream
{
    IObservable<IMessage> Messages { get; }

    Task Push(IMessage message);
}