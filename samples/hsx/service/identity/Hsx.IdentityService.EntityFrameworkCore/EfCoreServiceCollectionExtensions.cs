using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleDomain.Repositories;
using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Repositories;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Repositories;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Repositories;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Entities;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Repositories;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Entities;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Repositories;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Entities;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Repositories;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Hsx.IdentityService.Domain.CompanyDomain.Repositories;
using Hsx.IdentityService.Domain.CustomerDomain.Entities;
using Hsx.IdentityService.Domain.CustomerDomain.Repositories;
using Hsx.IdentityService.Domain.MenuDomain.Entities;
using Hsx.IdentityService.Domain.MenuDomain.Repositories;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Repositories;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.SubscriptionDomain.Repositories;
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
    /// Registers what the identity service needs of PostgreSQL: the <see cref="IdentityServiceDbContext"/> on the provider and the connection string that <see cref="EfCoreDbProperties"/> names (<c>ConnectionStrings:EfCore-IdentityService</c>),
    /// with its readiness check, the repository of every entity (each also as the generic <c>IRepository</c> of its entity) and the unit of work. The context itself is used by the repositories and the seeders only. It prepares nothing: the host that owns the database adds <see cref="AddServiceEfCoreDatabaseStartup"/>.
    /// </summary>
    public static IServiceCollection AddServiceEfCoreDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddBaseEfCore<IdentityServiceDbContext>(configuration, providers => providers.UsePostgres(), EfCoreDbProperties.Provider, EfCoreDbProperties.ConnectionStringName);
        services.AddBaseMultiTenancyEntityFramework();
        services.AddEfCoreUnitOfWork<IdentityServiceDbContext>();

        services.AddRepository<ICompanyRepository, EfCoreCompanyRepository, Company>();
        services.AddRepository<ICustomerRepository, EfCoreCustomerRepository, Customer>();
        services.AddRepository<IPermissionRepository, EfCorePermissionRepository, Permission>();
        services.AddRepository<IAppRolePermissionRepository, EfCoreAppRolePermissionRepository, AppRolePermission>();
        services.AddRepository<IAppRolePermissionConstraintRepository, EfCoreAppRolePermissionConstraintRepository, AppRolePermissionConstraint>();
        services.AddRepository<IAppMenuPermissionRepository, EfCoreAppMenuPermissionRepository, AppMenuPermission>();
        services.AddRepository<IAuthLoginAuditRepository, EfCoreAuthLoginAuditRepository, AuthLoginAudit>();
        services.AddRepository<IAppUserRepository, EfCoreAppUserRepository, AppUser>();
        services.AddRepository<IAppRoleRepository, EfCoreAppRoleRepository, AppRole>();
        services.AddRepository<IAppUserRoleRepository, EfCoreAppUserRoleRepository, AppUserRole>();
        services.AddRepository<IAppRoleSubscriptionRepository, EfCoreAppRoleSubscriptionRepository, AppRoleSubscription>();
        services.AddRepository<ISubscriptionRepository, EfCoreSubscriptionRepository, Subscription>();
        services.AddRepository<ITenantRepository, EfCoreTenantRepository, Tenant>();
        services.AddRepository<IAuthRefreshTokenRepository, EfCoreAuthRefreshTokenRepository, AuthRefreshToken>();
        services.AddRepository<IAuthPasswordPolicyRepository, EfCoreAuthPasswordPolicyRepository, AuthPasswordPolicy>();
        services.AddRepository<IAppMenuRepository, EfCoreAppMenuRepository, AppMenu>();
        services.AddScoped<IPermissionGrantRepository, EfCorePermissionGrantRepository>();

        return services;
    }

    /// <summary>
    /// What prepares the database when the host starts, <b>for the host that owns it</b> (the one whose <c>SeedStartupConnections</c> names it; a host that only reads the database, the auth server, does not call this and so needs neither the permission catalog nor the seeders):
    /// the steps run in the order they are added, the schema first (the migrations of this project, in their order), then the data.
    /// </summary>
    public static IServiceCollection AddServiceEfCoreDatabaseStartup(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMigrationStartupStep<IdentityServiceDbContext>(EfCoreDbProperties.ConnectionStringName);
        services.AddDataSeeder<TenantsSeeder>(EfCoreDbProperties.ConnectionStringName);       // the system and the resellers, which the roles and the users belong to
        services.AddDataSeeder<CompaniesSeeder>(EfCoreDbProperties.ConnectionStringName);     // the companies the customers belong to
        services.AddDataSeeder<CustomersSeeder>(EfCoreDbProperties.ConnectionStringName);     // the customers of the companies
        services.AddDataSeeder<PermissionsSeeder>(EfCoreDbProperties.ConnectionStringName);   // the permissions of the code and the menu, before the roles that are granted them
        services.AddDataSeeder<RolesSeeder>(EfCoreDbProperties.ConnectionStringName);         // the roles of the tenants, with the starting grants of their type: after the permissions they are granted
        services.AddDataSeeder<UsersSeeder>(EfCoreDbProperties.ConnectionStringName);         // one user for each of those roles, with no password anyone knows
        return services;
    }

    /// <summary>The repository of an entity: its own interface and the generic <see cref="IRepository{TEntity, TKey}"/> are the same scoped object.</summary>
    private static void AddRepository<TInterface, TImplementation, TEntity>(this IServiceCollection services)
        where TInterface : class, IRepository<TEntity, Guid>
        where TImplementation : class, TInterface
        where TEntity : class, IEntity<Guid>
    {
        services.AddScoped<TImplementation>();
        services.AddScoped<TInterface>(provider => provider.GetRequiredService<TImplementation>());
        services.AddScoped<IRepository<TEntity, Guid>>(provider => provider.GetRequiredService<TImplementation>());
    }
}
