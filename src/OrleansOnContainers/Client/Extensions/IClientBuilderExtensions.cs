using Client.Options;
using Microsoft.Extensions.Configuration;
using Shared;
using Shared.Helpers;
using Shared.Options;

namespace Client.Extensions;

public static class IClientBuilderExtensions
{
    public static IClientBuilder ConfigureClustering(this IClientBuilder builder, IConfiguration configuration)
    {
        var clientOptions = new ClientOptions();
        configuration
            .GetRequiredSection(ClientOptions.Key)
            .Bind(clientOptions);

        return builder.ConfigureClustering(configuration, clientOptions.ClusteringProvider!);
    }

    private static IClientBuilder ConfigureAdoNetClustering(this IClientBuilder builder, IConfiguration configuration)
    {
        var adoNetOptions = new AdoNetProvider();
        configuration
            .GetRequiredSection("AdoNetProvider")
            .Bind(adoNetOptions);
        builder.UseAdoNetClustering(options =>
        {
            options.Invariant = adoNetOptions.Invariant;
            options.ConnectionString = adoNetOptions.ConnectionString;
        });

        return builder;
    }

    private static IClientBuilder ConfigureClustering(this IClientBuilder builder, IConfiguration configuration, string clusteringProvider) =>
        clusteringProvider.ToLowerInvariant() switch
        {
            ClusteringProviders.AdoNet => builder.ConfigureAdoNetClustering(configuration),
            ClusteringProviders.Development => builder.ConfigureStaticClustering(configuration),
            _ => throw new ArgumentException($"'{clusteringProvider}' is not a supported clustering provider.", nameof(clusteringProvider))
        };

    private static IClientBuilder ConfigureStaticClustering(this IClientBuilder builder, IConfiguration configuration)
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
