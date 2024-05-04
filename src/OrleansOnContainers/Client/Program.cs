using Client.Application.DependencyInjection;
using Client.Application.Options;
using Client.Extensions;
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
        client.ConfigureClustering(configuration);
    })
    .ConfigureServices(serviceCollection =>
    {
        // These want to be moved to the Application project registration section
        serviceCollection.Configure<ResubscriberOptions>(configuration.GetSection(ResubscriberOptions.Key));
        serviceCollection.AddSingleton(TimeProvider.System);

        serviceCollection.AddChatServices();
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