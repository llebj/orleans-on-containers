using Shared.Messages;

namespace Client.Services;

internal interface IMessageStream
{
    IObservable<OldChatMessage> Messages { get; }

    Task Push(OldChatMessage message);
}