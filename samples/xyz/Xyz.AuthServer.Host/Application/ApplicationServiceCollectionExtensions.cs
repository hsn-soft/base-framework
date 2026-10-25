using Xyz.AuthServer.Application.Contracts.UserDomain.Services;
using Xyz.AuthServer.Application.Contracts.UserHistoryDomain.Services;
using Xyz.AuthServer.Application.Services;

namespace Xyz.AuthServer.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers the application services of the identity service. The repositories come from the persistence project.</summary>
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IUserAppService, UserAppService>();
        services.AddScoped<IUserHistoryAppService, UserHistoryAppService>();
        return services;
    }
}
