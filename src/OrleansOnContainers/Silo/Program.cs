using Grains.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Extensions;
using Shared.Messages;
using Silo.Extensions;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

var builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo.ConfigureClustering(configuration);
        silo.Services.AddJsonSerializerForAssembly(typeof(ChatMessage));
    })
    .ConfigureServices(serviceCollection =>
    {
        serviceCollection.Configure<ChatGrainOptions>(configuration.GetSection(ChatGrainOptions.Key));
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConfiguration(configuration);
        logging.AddConsole();
    })
    .UseConsoleLifetime();

using IHost host = builder.Build();

await host.RunAsync();