using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Entities;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Entities;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Entities;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Hsx.IdentityService.Domain.CustomerDomain.Entities;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Hsx.IdentityService.EntityFrameworkCore.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Context;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
    : MultiTenantDbContext(options, dataFilter, currentTenant)
{
    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<AppUser> AppUsers => Set<AppUser>();

    public DbSet<AppRole> AppRoles => Set<AppRole>();

    public DbSet<AppUserRole> AppUserRoles => Set<AppUserRole>();

    public DbSet<AppRoleSubscription> AppRoleSubscriptions => Set<AppRoleSubscription>();

    public DbSet<AuthPasswordPolicy> AuthPasswordPolicies => Set<AuthPasswordPolicy>();

    public DbSet<AuthRefreshToken> AuthRefreshTokens => Set<AuthRefreshToken>();

    public DbSet<AuthLoginAudit> AuthLoginAudits => Set<AuthLoginAudit>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureCompanyDomain();
        modelBuilder.ConfigureCustomerDomain();
        modelBuilder.ConfigureSubscriptionDomain();
        modelBuilder.ConfigureTenantDomain();
        modelBuilder.ConfigureAppUserDomain();
        modelBuilder.ConfigureAppRoleDomain();
        modelBuilder.ConfigureAppUserRoleDomain();
        modelBuilder.ConfigureAppRoleSubscriptionDomain();
        modelBuilder.ConfigureAuthPasswordPolicyDomain();
        modelBuilder.ConfigureAuthRefreshTokenDomain();
        modelBuilder.ConfigureAuthLoginAuditDomain();
    }
}
