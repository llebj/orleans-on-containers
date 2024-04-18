using Client.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Client.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChatServices(this IServiceCollection services) 
    {
        services.AddSingleton<IResubscriberManager, ResubscriberManager>();
        services.AddSingleton<IObserverManager, ObserverManager>();
        services.AddSingleton<IChatAccessClient, ChatAccessClient>();
        services.AddSingleton<IMessageClient, MessageClient>();
        services.AddMessageStream();

        return services;
    }

    internal static IServiceCollection AddMessageStream(this IServiceCollection services)
    {
        services.AddSingleton<MessageStream>();
        services.AddSingleton<IMessageStreamOutput, MessageStream>(f => f.GetRequiredService<MessageStream>());
        services.AddSingleton<IMessageStreamInput, MessageStream>(f => f.GetRequiredService<MessageStream>());

        return services;
    }
}
