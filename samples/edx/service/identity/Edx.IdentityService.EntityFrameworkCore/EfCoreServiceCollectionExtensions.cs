using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using Edx.IdentityService.EntityFrameworkCore.Context;
using Edx.IdentityService.EntityFrameworkCore.Repositories;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edx.IdentityService.EntityFrameworkCore;

public static class EfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the identity service needs of its database: the <c>IdentityDbContext</c> on the provider and the connection string that <see cref="EfCoreIdentityDbProperties"/> names (SQL Server,
    /// <c>ConnectionStrings:EfCore-IdentityService</c>), with its readiness check, the tenants of the rows (<c>AddBaseMultiTenancyEntityFramework</c>) and the repositories.
    /// </summary>
    public static IServiceCollection AddIdentityPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseEfCore<IdentityDbContext>(configuration, providers => providers.UseSqlServer(), EfCoreIdentityDbProperties.Provider, EfCoreIdentityDbProperties.ConnectionStringName);
        services.AddBaseMultiTenancyEntityFramework();

        services.AddScoped<IUserRepository, EfCoreUserRepository>();
        services.AddScoped<IRepository<User, Guid>>(provider => provider.GetRequiredService<IUserRepository>());
        services.AddScoped<IPermissionGrantRepository, EfCorePermissionGrantRepository>();
        services.AddScoped<IMenuRepository, EfCoreMenuRepository>();
        services.AddEfCoreRepository<IdentityDbContext, Role, Guid>();
        services.AddEfCoreRepository<IdentityDbContext, Permission, Guid>();
        return services.AddEfCoreRepository<IdentityDbContext, Tenant, Guid>();
    }
}
