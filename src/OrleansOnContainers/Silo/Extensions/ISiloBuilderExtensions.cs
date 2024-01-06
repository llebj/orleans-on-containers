using Microsoft.Extensions.Configuration;
using Shared.Helpers;
using Shared.Options;
using Silo.Options;

namespace Silo.Extensions;

public static class ISiloBuilderExtensions
{
    public static ISiloBuilder ConfigureClustering(this ISiloBuilder builder, IConfiguration configuration)
    {
        var siloOptions = new SiloOptions();
        configuration
            .GetRequiredSection(SiloOptions.Key)
            .Bind(siloOptions);

        return builder.ConfigureClustering(configuration, siloOptions.ClusteringProvider!);
    }

    private static ISiloBuilder ConfigureAdoNetClustering(this ISiloBuilder builder, IConfiguration configuration)
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

    private static ISiloBuilder ConfigureClustering(this ISiloBuilder builder, IConfiguration configuration, string clusteringProvider) =>
        clusteringProvider.ToLowerInvariant() switch
        {
            "adonet" => builder.ConfigureAdoNetClustering(configuration),
            "development" => builder.ConfigureDevelopmentClustering(configuration),
            _ => throw new ArgumentException($"'{clusteringProvider}' is not a supported clustering provider.", nameof(clusteringProvider))
        };

    private static ISiloBuilder ConfigureDevelopmentClustering(this ISiloBuilder builder, IConfiguration configuration)
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
