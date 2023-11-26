using Microsoft.Extensions.Configuration;
using Shared.Helpers;
using Shared.Options;

namespace Silo.Extensions;

public static class ISiloBuilderExtensions
{
    public static ISiloBuilder ConfigureDevelopmentClustering(this ISiloBuilder builder, IConfiguration configuration)
    {
        var primarySiloSettings = new SiloEndpointOptions();
        configuration
            .GetRequiredSection("PrimarySilo")
            .Bind(primarySiloSettings);
        var ipEndpoint = NetworkHelpers.GetIpEndpoint(primarySiloSettings.HostName!, primarySiloSettings.Port);
        builder.UseDevelopmentClustering(ipEndpoint);

        return builder;
    }    
}
