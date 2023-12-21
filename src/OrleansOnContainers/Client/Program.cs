using Client;
using Client.Extensions;
using Client.Options;
using Client.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

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
    })
    .ConfigureServices(serviceCollection =>
    {
        serviceCollection.Configure<ClientOptions>(options => options.ClientId = Guid.NewGuid());
        serviceCollection.Configure<ObserverManagerOptions>(configuration.GetSection(ObserverManagerOptions.Key));
        serviceCollection.AddScoped<IChatService, ChatService>();
        serviceCollection.AddScoped<IGrainObserverManager, GrainObserverManager>();
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