using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;
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
    }
}
