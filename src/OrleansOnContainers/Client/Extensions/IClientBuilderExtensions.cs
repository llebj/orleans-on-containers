using Microsoft.Extensions.Configuration;
using Shared.Helpers;
using Shared.Options;

namespace Client.Extensions;

public static class IClientBuilderExtensions
{
    public static IClientBuilder ConfigureStaticClustering(this IClientBuilder builder, IConfiguration configuration)
    {
        var endpoints = new List<SiloEndpointOptions>();
        configuration
            .GetRequiredSection("SiloEndpoints")
            .Bind(endpoints);
        var ipEndpoints = endpoints
            .Select(endpoint => NetworkHelpers.GetIpEndpoint(endpoint.HostName!, endpoint.Port))
            .ToArray();
        builder.UseStaticClustering(ipEndpoints);

        return builder;
    }
}
