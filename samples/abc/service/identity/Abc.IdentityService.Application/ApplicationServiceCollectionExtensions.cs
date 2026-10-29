using Abc.IdentityService.Application.Contracts.UserDomain.Services;
using Abc.IdentityService.Application.Contracts.UserHistoryDomain.Services;
using Abc.IdentityService.Application.Services;
using HsnSoft.Base.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.IdentityService.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers the application services of the identity service. The repositories come from the persistence project.</summary>
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // the most a client may ask for in a list (the Paging section)
        services.AddBasePaging(configuration);

        services.AddScoped<IUserAppService, UserAppService>();
        services.AddScoped<IUserHistoryAppService, UserHistoryAppService>();
        return services;
    }
}
