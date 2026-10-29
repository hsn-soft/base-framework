using Edx.Integration.FirmInvoice.Application.Contracts.FirmInvoiceDomain.Services;
using Edx.Integration.FirmInvoice.Application.Services;
using Edx.Integration.FirmInvoice.Application.Storage;
using Edx.Integration.FirmInvoice.Application.Uploads;
using HsnSoft.Base.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edx.Integration.FirmInvoice.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the application services of the firm invoice integration. The repositories come from the persistence project, the blob store (<c>IBlobStore</c>) and the event bus from the host. Where the files are kept is the
    /// <c>Storage</c> section (<see cref="StorageOptions"/>), what is accepted the <c>FirmInvoice</c> section (<see cref="FirmInvoiceOptions"/>).
    /// </summary>
    public static IServiceCollection AddFirmInvoiceApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<FirmInvoiceOptions>(configuration.GetSection(FirmInvoiceOptions.SectionName));

        // the most a client may ask for in a list (the Paging section)
        services.AddBasePaging(configuration);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<UploadInspector>();
        services.AddScoped<StorageLayout>();
        services.AddScoped<IFirmInvoiceAppService, FirmInvoiceAppService>();
        services.AddScoped<QueueUpdater>();
        return services;
    }
}
