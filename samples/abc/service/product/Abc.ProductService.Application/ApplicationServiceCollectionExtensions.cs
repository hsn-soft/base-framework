using Abc.ProductService.Application.Contracts.ProductDomain.Services;
using Abc.ProductService.Application.Contracts.ProductStockHistoryDomain.Services;
using Abc.ProductService.Application.EventHandlers;
using Abc.ProductService.Application.Services;
using Abc.Shared.Events;
using HsnSoft.Base.EventBus;
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

        // a new order of the order service: the stock is checked (the handler runs in its own scope, per message)
        services.AddEventHandler<OrderCreatedEto, OrderCreatedHandler>();
        return services;
    }
}
