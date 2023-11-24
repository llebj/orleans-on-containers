using Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args)
    .UseOrleansClient(client =>
    {
        client.UseLocalhostClustering();
    })
    .ConfigureServices(serviceCollection =>
    {
        serviceCollection.AddHostedService<ChatHostedService>();
    })
    .ConfigureLogging(logging => logging.AddConsole())
    .UseConsoleLifetime();

using IHost host = builder.Build();

await host.RunAsync();