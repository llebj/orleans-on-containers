using Client;
using Client.Extensions;
using Client.Options;
using Client.Services;
using GrainInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Shared;
using Shared.Extensions;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

var logger = new LoggerConfiguration()
    .ReadFrom
    .Configuration(configuration)
    .CreateLogger();

var builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.ConfigureStaticClustering(configuration);

        // A System.Text.Json serializer has to be used here as I was unable to get the orleans serialization to work.
        // I attempted to use the GenerateSerializerAttribute coupled with IdAttribute, however the code generator
        // was unable to find the copier as the ChatMessage class resides in a different assembly.
        // I then tried using the GenerateCodeForDeclaringAssembly to generate for ChatMessage, but that required
        // public setters even though I declared the type as being Immutable and even then, it couldn't locate the
        // copier.
        client.Services.AddJsonSerializerForAssembly(typeof(ChatMessage));
    })
    .ConfigureServices(serviceCollection =>
    {
        serviceCollection.Configure<ObserverManagerOptions>(configuration.GetSection(ObserverManagerOptions.Key));
        serviceCollection.AddScoped<IChatObserver, ChatObserver>();
        serviceCollection.AddScoped<IChatService, ChatService>();
        serviceCollection.AddScoped<IGrainObserverManager, GrainObserverManager>();
        serviceCollection.AddScoped<IMessageStream, MessageStream>();
        serviceCollection.AddScoped<IResubscriber<GrainSubscription>, ResubscriptionTimer<GrainSubscription>>();
        serviceCollection.AddSingleton(TimeProvider.System);
        serviceCollection.AddHostedService<ChatHostedService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddSerilog(logger);
    })
    .UseConsoleLifetime();

using IHost host = builder.Build();

await host.RunAsync();