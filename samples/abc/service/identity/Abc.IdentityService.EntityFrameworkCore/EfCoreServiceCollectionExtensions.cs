using Abc.IdentityService.Domain.UserDomain.Repositories;
using Abc.IdentityService.EntityFrameworkCore.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Abc.IdentityService.EntityFrameworkCore;

public static class EfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the repositories of the identity service. The <c>IdentityDbContext</c> itself is registered by the host with the
    /// database provider it chose (<c>AddBasePostgres&lt;IdentityDbContext&gt;</c>).
    /// </summary>
    public static IServiceCollection AddIdentityPersistence(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IUserRepository, EfCoreUserRepository>();
        return services;
    }
}
