using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Shared.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddJsonSerializerForAssembly(this IServiceCollection serviceCollection, Type assemblyType)
    {
        var assemblyName = assemblyType.Assembly.GetName().Name;
        serviceCollection.AddSerializer(serializerBuilder =>
        {
            serializerBuilder.AddJsonSerializer(
                isSupported: type => type.Namespace!.StartsWith(assemblyName!));
        });

        return serviceCollection;
    }
}
