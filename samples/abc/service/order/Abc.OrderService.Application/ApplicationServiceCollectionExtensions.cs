using Abc.OrderService.Application.Contracts.OrderDomain.Services;
using Abc.OrderService.Application.EventHandlers;
using Abc.OrderService.Application.Services;
using Abc.Shared.Events;
using HsnSoft.Base.EventBus;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.OrderService.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers the application services of the order service (they are added here as the use cases are). The repositories come from the persistence project.</summary>
    public static IServiceCollection AddOrderApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IOrderAppService, OrderAppService>();

        // the answers of the other services to an order: each handler runs in its own scope, per message
        services.AddEventHandler<OrderStockReservedEto, OrderStockReservedHandler>();
        services.AddEventHandler<OrderStockRejectedEto, OrderStockRejectedHandler>();
        services.AddEventHandler<ShipmentCompletedEto, ShipmentCompletedHandler>();
        return services;
    }
}
