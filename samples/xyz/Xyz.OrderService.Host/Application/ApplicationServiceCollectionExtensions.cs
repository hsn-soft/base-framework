using Xyz.OrderService.Application.Contracts.OrderDomain.Services;
using Xyz.OrderService.Application.Services;

namespace Xyz.OrderService.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers the application services of the order service (they are added here as the use cases are). The repositories come from the persistence project.</summary>
    public static IServiceCollection AddOrderApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IOrderAppService, OrderAppService>();
        return services;
    }
}
