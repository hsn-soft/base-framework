using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Hsx.IdentityService.EntityFrameworkCore.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Context;

/// <summary>The identity database: the tenants and what they sell (companies, customers, subscriptions), the users and their roles, the grants and the menu, and what the sign-in keeps (the policy, the refresh tokens, the audit). Only the repositories and the seeders use it.</summary>
public sealed class IdentityServiceDbContext(DbContextOptions<IdentityServiceDbContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
    : MultiTenantDbContext(options, dataFilter, currentTenant)
{
    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureAppRole();
        modelBuilder.ConfigureAppRoleSubscription();
        modelBuilder.ConfigureAppUser();
        modelBuilder.ConfigureAppUserRole();
        modelBuilder.ConfigureAuthLoginAudit();
        modelBuilder.ConfigureAuthPasswordPolicy();
        modelBuilder.ConfigureAuthRefreshToken();
        modelBuilder.ConfigureCompany();
        modelBuilder.ConfigureCustomer();
        modelBuilder.ConfigureAppMenu();
        modelBuilder.ConfigureAppMenuPermission();
        modelBuilder.ConfigurePermission();
        modelBuilder.ConfigureAppRolePermission();
        modelBuilder.ConfigureAppRolePermissionConstraint();
        modelBuilder.ConfigureSubscription();
        modelBuilder.ConfigureTenant();
    }
}
