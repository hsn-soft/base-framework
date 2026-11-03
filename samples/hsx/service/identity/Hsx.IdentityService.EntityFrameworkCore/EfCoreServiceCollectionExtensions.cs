using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Repositories;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Entities;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Entities;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Repositories;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Repositories;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Hsx.IdentityService.Domain.CustomerDomain.Entities;
using Hsx.IdentityService.Domain.MenuDomain.Repositories;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Repositories;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Context;
using Hsx.IdentityService.EntityFrameworkCore.Repositories;
using Hsx.IdentityService.EntityFrameworkCore.Seeding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hsx.IdentityService.EntityFrameworkCore;

public static class EfCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the identity service needs of PostgreSQL: the <c>IdentityDbContext</c> on the provider and the connection string that <see cref="EfCoreIdentityDbProperties"/> names (<c>ConnectionStrings:EfCore-IdentityService</c>), with its readiness check and the repositories.
    /// </summary>
    public static IServiceCollection AddIdentityPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseEfCore<IdentityDbContext>(configuration, providers => providers.UsePostgres(), EfCoreIdentityDbProperties.Provider, EfCoreIdentityDbProperties.ConnectionStringName);
        services.AddBaseMultiTenancyEntityFramework();
        services.AddEfCoreRepository<IdentityDbContext, Company, Guid>();
        services.AddEfCoreRepository<IdentityDbContext, Customer, Guid>();
        services.AddEfCoreRepository<IdentityDbContext, Subscription, Guid>();
        services.AddEfCoreRepository<IdentityDbContext, Tenant, Guid>();
        services.AddEfCoreRepository<IdentityDbContext, AppUser, Guid>();
        services.AddEfCoreRepository<IdentityDbContext, AppRole, Guid>();
        services.AddEfCoreRepository<IdentityDbContext, AppUserRole, Guid>();
        services.AddEfCoreRepository<IdentityDbContext, AppRoleSubscription, Guid>();
        services.AddEfCoreRepository<IdentityDbContext, Permission, Guid>();
        services.AddEfCoreRepository<IdentityDbContext, AuthPasswordPolicy, Guid>();
        services.AddEfCoreRepository<IdentityDbContext, AuthLoginAudit, Guid>();

        services.AddScoped<ITenantRepository, EfCoreTenantRepository>();
        services.AddScoped<ISignInRepository, EfCoreSignInRepository>();
        services.AddScoped<IPermissionGrantRepository, EfCorePermissionGrantRepository>();
        services.AddScoped<IMenuRepository, EfCoreMenuRepository>();
        services.AddScoped<IRefreshTokenStore, EfCoreRefreshTokenStore>();
        services.AddScoped<IPasswordPolicyProvider, EfCorePasswordPolicyProvider>();

        // the schema is migrated when the host starts, by the host that owns the database (SeedStartupConnections): the migrations of this project, in their order
        services.AddMigrationStartupStep<IdentityDbContext>(EfCoreIdentityDbProperties.ConnectionStringName);
        services.AddDataSeeder<PermissionsSeeder>(EfCoreIdentityDbProperties.ConnectionStringName);   // the permissions of the code and the menu, before the roles that are granted them
        services.AddDataSeeder<SampleIdentitySeeder>(EfCoreIdentityDbProperties.ConnectionStringName);   // the demo tenants, customer, subscriptions, roles and users (off with Seeding:Disabled)
        return services;
    }
}
