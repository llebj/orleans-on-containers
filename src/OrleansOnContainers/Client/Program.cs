using Client;
using Client.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

var builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.ConfigureStaticClustering(configuration);
    })
    .ConfigureServices(serviceCollection =>
    {
        serviceCollection.AddHostedService<ChatHostedService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConfiguration(configuration);
        logging.AddConsole();
    })
    .UseConsoleLifetime();

using IHost host = builder.Build();

await host.RunAsync();