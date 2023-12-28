using Shared;

namespace Client.Services;

internal interface IMessageStream
{
    IObservable<ChatMessage> Messages { get; }

    Task Push(ChatMessage message);
}