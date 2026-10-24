using Abc.OrderService.Application.Contracts.OrderDomain.Services;
using Abc.OrderService.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Abc.OrderService.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers the application services of the order service (they are added here as the use cases are). The repositories come from the persistence project.</summary>
    public static IServiceCollection AddOrderApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IOrderAppService, OrderAppService>();
        return services;
    }
}
