using Edx.InvoiceService.Application.Contracts.InvoiceDomain.Services;
using Edx.InvoiceService.Application.Contracts.InvoiceReportDomain.Services;
using Edx.InvoiceService.Application.Contracts.PurchaseInvoiceDomain.Services;
using Edx.InvoiceService.Application.Importing;
using Edx.InvoiceService.Application.Services;
using Edx.InvoiceService.Application.Storage;
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

        services.Configure<InvoiceStorageOptions>(configuration.GetSection(InvoiceStorageOptions.SectionName));
        services.Configure<InvoiceImportOptions>(configuration.GetSection(InvoiceImportOptions.SectionName));

        // the most a client may ask for in a list (the Paging section)
        services.AddBasePaging(configuration);

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<InvoiceImportService>();
        services.AddScoped<ISalesInvoiceAppService, SalesInvoiceAppService>();
        services.AddScoped<IPurchaseInvoiceAppService, PurchaseInvoiceAppService>();
        services.AddScoped<PurchaseInvoiceReceiver>();
        services.AddScoped<SalesInvoiceAnswerReceiver>();
        services.AddScoped<IInvoiceReportAppService, InvoiceReportAppService>();
        return services;
    }
}
