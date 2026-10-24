using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Abc.ShipmentService.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers the application services of the shipment service (they are added here as the use cases are). The repositories come from the persistence project.</summary>
    public static IServiceCollection AddShipmentApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
