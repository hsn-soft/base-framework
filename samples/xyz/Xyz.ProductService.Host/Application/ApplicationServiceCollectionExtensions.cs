using Xyz.ProductService.Application.Contracts.ProductDomain.Services;
using Xyz.ProductService.Application.Contracts.ProductStockHistoryDomain.Services;
using Xyz.ProductService.Application.Services;

namespace Xyz.ProductService.Application;

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
