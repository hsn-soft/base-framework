using Hhs.Shared.Contracts.Cache;
using Hhs.VideoGeneratorService.Domain.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.VideoGeneratorService.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddServiceApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(typeof(ApplicationAutoMapperProfile));

        services.Configure<VideoRequestQuerySettings>(configuration.GetSection(nameof(VideoRequestQuerySettings)));
        services.Configure<VideoGenerationSettings>(configuration.GetSection(nameof(VideoGenerationSettings)));

        services.AddSingleton<IServicePermissionProvider, ApplicationPermissionProvider>();

        return services;
    }
}