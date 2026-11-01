using HsnSoft.Base.Application;
using Hsx.ContentService.Application.Contracts.AnalysisContentDomain.Services;
using Hsx.ContentService.Application.Contracts.AnalysisContentItemDomain.Services;
using Hsx.ContentService.Application.Contracts.CustomerContentDomain.Services;
using Hsx.ContentService.Application.Contracts.CustomerContentVisitDomain.Services;
using Hsx.ContentService.Application.Contracts.CustomerVpSettingDomain.Services;
using Hsx.ContentService.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hsx.ContentService.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers the application services of the content service. The repositories come from the persistence project.</summary>
    public static IServiceCollection AddContentApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // the most a client may ask for in a list (the Paging section)
        services.AddBasePaging(configuration);
        services.AddScoped<ICustomerContentAppService, CustomerContentAppService>();
        services.AddScoped<IAnalysisContentAppService, AnalysisContentAppService>();
        services.AddScoped<IAnalysisContentItemAppService, AnalysisContentItemAppService>();
        services.AddScoped<ICustomerContentVisitAppService, CustomerContentVisitAppService>();
        services.AddScoped<ICustomerVpSettingAppService, CustomerVpSettingAppService>();
        return services;
    }
}
