using Abc.ProductService.Application.Contracts.ProductDomain.Services;
using Abc.ProductService.Application.Contracts.ProductStockHistoryDomain.Services;
using Abc.ProductService.Application.Services;
using HsnSoft.Base.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.ProductService.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers the application services of the product service . The repositories come from the persistence project.</summary>
    public static IServiceCollection AddProductApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // the most a client may ask for in a list (the Paging section)
        services.AddBasePaging(configuration);

        services.AddScoped<IProductAppService, ProductAppService>();
        services.AddScoped<IProductStockHistoryAppService, ProductStockHistoryAppService>();
        services.AddScoped<StockReservationService>();
        return services;
    }
}
