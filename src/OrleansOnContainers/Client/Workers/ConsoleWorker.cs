using Client.Services;

namespace Client.Workers;

internal class ConsoleWorker
{
    private readonly IChatService _chatService;

    public ConsoleWorker(
        IChatService chatService)
    {
        _chatService = chatService;
    }
}
