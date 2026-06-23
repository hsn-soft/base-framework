using Hhs.ContentService.Application.Services;
using Hhs.ContentService.Domain.Settings;
using Hhs.Shared.Contracts.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.ContentService.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddServiceApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(typeof(ApplicationAutoMapperProfile));

        services.Configure<ContentOperationSettings>(configuration.GetSection(nameof(ContentOperationSettings)));

        services.AddSingleton<IServicePermissionProvider, ApplicationPermissionProvider>();

        // Must be Scoped or Transient => Cannot consume any scoped service
        // services.AddScoped<ContentInboxStore>();
        services.AddScoped<ContentOperationAppService>();

        return services;
    }
}