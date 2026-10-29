using Abc.OrderService.Application.Contracts.OrderDomain.Services;
using Abc.OrderService.Application.Services;
using HsnSoft.Base.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.OrderService.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers the application services of the order service (they are added here as the use cases are). The repositories come from the persistence project.</summary>
    public static IServiceCollection AddOrderApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // the most a client may ask for in a list (the Paging section)
        services.AddBasePaging(configuration);

        services.AddScoped<IOrderAppService, OrderAppService>();
        return services;
    }
}
