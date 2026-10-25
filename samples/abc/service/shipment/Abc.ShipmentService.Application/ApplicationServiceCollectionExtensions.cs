using Abc.ShipmentService.Application.Contracts.CustomerDomain.Services;
using Abc.ShipmentService.Application.Contracts.PaymentDomain.Services;
using Abc.ShipmentService.Application.Contracts.ShipmentDomain.Services;
using Abc.ShipmentService.Application.Contracts.ShipmentHistoryDomain.Services;
using Abc.ShipmentService.Application.Services;
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
        services.AddScoped<ICustomerAppService, CustomerAppService>();
        services.AddScoped<IPaymentAppService, PaymentAppService>();
        services.AddScoped<PaymentProcessor>();
        return services;
    }
}
