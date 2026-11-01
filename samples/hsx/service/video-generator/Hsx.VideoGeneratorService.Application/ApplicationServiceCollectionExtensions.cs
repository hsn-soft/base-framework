using HsnSoft.Base.Application;
using Hsx.VideoGeneratorService.Application.Contracts.VideoRequestDomain.Services;
using Hsx.VideoGeneratorService.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hsx.VideoGeneratorService.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers the application services of the video generator service. The repositories come from the persistence project.</summary>
    public static IServiceCollection AddVideoGeneratorApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // the most a client may ask for in a list (the Paging section)
        services.AddBasePaging(configuration);
        services.AddScoped<IVideoRequestAppService, VideoRequestAppService>();
        return services;
    }
}
