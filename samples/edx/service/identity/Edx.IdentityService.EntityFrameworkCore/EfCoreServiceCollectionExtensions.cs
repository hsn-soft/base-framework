using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using Edx.IdentityService.EntityFrameworkCore.Context;
using Edx.IdentityService.EntityFrameworkCore.Repositories;
using HsnSoft.Base.Data;
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
        services.AddEfCoreRepository<IdentityDbContext, Tenant, Guid>();

        // when the host starts, in this order, by the host that owns the database (SeedStartupConnections; the auth server, which reads it, does not): the schema, the tenants, the permissions of the catalogue (the roles
        // are given their starting grants when they are made, so the permissions come first), then the roles and the users of the sample
        string connection = EfCoreIdentityDbProperties.ConnectionStringName;
        services.AddSchemaStartupStep<IdentityDbContext>(connection);
        services.AddDatabaseStartupStep(connection, "IdentityDbContext tenants", IdentityDatabase.EnsureTenantsAsync);
        services.AddDatabaseStartupStep(connection, "IdentityDbContext permissions", PermissionDatabase.EnsurePermissionsAsync);
        return services.AddDatabaseStartupStep(connection, "IdentityDbContext sample identity", SampleIdentity.EnsureSampleIdentityAsync);
    }
}
