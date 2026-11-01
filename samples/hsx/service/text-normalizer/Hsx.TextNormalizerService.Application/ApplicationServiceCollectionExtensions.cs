using HsnSoft.Base.Application;
using Hsx.TextNormalizerService.Application.Contracts.CustomerContentNormalizedRequestDomain.Services;
using Hsx.TextNormalizerService.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hsx.TextNormalizerService.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers the application services of the text normalizer service. The repositories come from the persistence project.</summary>
    public static IServiceCollection AddTextNormalizerApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // the most a client may ask for in a list (the Paging section)
        services.AddBasePaging(configuration);
        services.AddScoped<ICustomerContentNormalizedRequestAppService, CustomerContentNormalizedRequestAppService>();
        return services;
    }
}
