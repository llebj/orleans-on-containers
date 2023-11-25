using Microsoft.Extensions.Configuration;
using Orleans.Hosting;
using Shared.Options;
using System.Net;

namespace Shared.Extensions;

public static class ISiloBuilderExtensions
{
    public static ISiloBuilder ConfigureDevelopmentClustering(this ISiloBuilder builder, IConfiguration configuration)
    {
        var primarySiloSettings = new PrimarySiloOptions();
        configuration
            .GetSection("PrimarySilo")
            .Bind(primarySiloSettings);
        var ipEndpoint = GetIpEndpoint(primarySiloSettings!.HostName, primarySiloSettings.Port);
        builder.UseDevelopmentClustering(ipEndpoint);

        return builder;
    }

    private static IPEndPoint GetIpEndpoint(string hostname, int port)
    {
        var host = Dns.GetHostEntry(hostname);

        return new IPEndPoint(host.AddressList[0], port);
    }
}
