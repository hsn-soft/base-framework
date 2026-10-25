using Abc.Shared.Events;
using Abc.ShipmentService.Application.Contracts.ShipmentDomain.Services;
using Abc.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Services;
using Abc.ShipmentService.Application.EventHandlers;
using Abc.ShipmentService.Application.Services;
using HsnSoft.Base.EventBus;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.ShipmentService.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers the application services of the shipment service (they are added here as the use cases are). The repositories come from the persistence project.</summary>
    public static IServiceCollection AddShipmentApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IShipmentAppService, ShipmentAppService>();
        services.AddScoped<IShipmentHistoryAppService, ShipmentHistoryAppService>();

        // the order service asks for a shipment once the stock is set aside (the handler runs in its own scope, per message)
        services.AddEventHandler<ShipmentRequestedEto, ShipmentRequestedHandler>();
        return services;
    }
}
