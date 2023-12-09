using Client.Options;
using Client.Services;
using Microsoft.Extensions.Options;

namespace Client.Workers;

internal class ConsoleWorker
{
    private readonly IChatService _chatService;
    private readonly ClientOptions _options;

    public ConsoleWorker(
        IChatService chatService,
        IOptions<ClientOptions> options)
    {
        _chatService = chatService;
        _options = options.Value;
    }
}
