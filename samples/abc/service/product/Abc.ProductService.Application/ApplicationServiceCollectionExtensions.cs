using Abc.ProductService.Application.Contracts.ProductDomain.Services;
using Abc.ProductService.Application.Contracts.ProductStockHistoryDomain.Services;
using Abc.ProductService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.ProductService.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers the application services of the product service . The repositories come from the persistence project.</summary>
    public static IServiceCollection AddProductApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IProductAppService, ProductAppService>();
        services.AddScoped<IProductStockHistoryAppService, ProductStockHistoryAppService>();
        services.AddScoped<StockReservationService>();
        return services;
    }
}
