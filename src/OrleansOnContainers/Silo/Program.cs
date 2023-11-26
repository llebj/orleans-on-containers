using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silo.Extensions;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

var builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo.ConfigureDevelopmentClustering(configuration);
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConfiguration(configuration);
        logging.AddConsole();
    })
    .UseConsoleLifetime();

using IHost host = builder.Build();

await host.RunAsync();