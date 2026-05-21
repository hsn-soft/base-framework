using Hhs.AuthServer.Application.Contracts.AuthDomainBackup.Interfaces;
using Hhs.AuthServer.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.AuthServer.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddServiceApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(typeof(ApplicationAutoMapperProfile));

        // Must be Scoped or Transient => Cannot consume any scoped service
        services.AddScoped<IUserAppService, UserAppService>();

        return services;
    }
}