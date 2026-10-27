using Edx.InvoiceService.Application.Contracts.ReceivedFileDomain.Services;
using Edx.InvoiceService.Application.Services;
using HsnSoft.Base.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edx.InvoiceService.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers the application services of the invoice service. The repositories come from the persistence project.</summary>
    public static IServiceCollection AddInvoiceApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // the most a client may ask for in a list (the Paging section)
        services.AddBasePaging(configuration);

        services.AddScoped<IReceivedFileAppService, ReceivedFileAppService>();
        return services;
    }
}
