using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using Hhs.IdentityService.Domain.AppUserDomain.Entities;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.TenantDomain.Entities;
using Hhs.IdentityService.EntityFrameworkCore.Configurations;
using HsnSoft.Base;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hhs.IdentityService.EntityFrameworkCore.Context;

public sealed class IdentityServiceDbContext(
    IServiceProvider provider,
    DbContextOptions<IdentityServiceDbContext> options
) : BaseEfCoreDbContext<IdentityServiceDbContext>(options, provider)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AppRole> AppRoles => Set<AppRole>();
    public DbSet<AppUserRole> AppUserRoles => Set<AppUserRole>();
    public DbSet<AppUserClaim> AppUserClaims => Set<AppUserClaim>();
    public DbSet<AppRoleClaim> AppRoleClaims => Set<AppRoleClaim>();
    public DbSet<AppRoleSubscription> AppRoleSubscriptions => Set<AppRoleSubscription>();
    public DbSet<AuthRefreshToken> AuthRefreshTokens => Set<AuthRefreshToken>();
    public DbSet<AuthLoginAudit> AuthLoginAudits => Set<AuthLoginAudit>();
    public DbSet<AuthPasswordPolicy> AuthPasswordPolicies => Set<AuthPasswordPolicy>();
    public DbSet<AuthEmailConfirmationToken> AuthEmailConfirmationTokens => Set<AuthEmailConfirmationToken>();
    public DbSet<AuthTokenRevocation> AuthTokenRevocations => Set<AuthTokenRevocation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        base.OnModelCreating(builder);

        builder.ConfigureTenantEntity();
        builder.ConfigureCompanyEntity();
        builder.ConfigureCustomerEntity();
        builder.ConfigureProductTypeEntity();
        builder.ConfigureSubscriptionEntity();

        builder.ConfigureAppUserEntity();
        builder.ConfigureAppRoleEntity();
        builder.ConfigureAppUserRoleEntity();
        builder.ConfigureAppUserClaimEntity();
        builder.ConfigureAppRoleClaimEntity();
        builder.ConfigureAppRoleSubscriptionEntity();
        builder.ConfigureAuthRefreshTokenEntity();
        builder.ConfigureAuthLoginAuditEntity();
        builder.ConfigureAuthPasswordPolicyEntity();
        builder.ConfigureAuthEmailConfirmationTokenEntity();
        builder.ConfigureAuthTokenRevocationEntity();
    }
}