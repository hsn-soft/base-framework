using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using Hhs.IdentityService.Domain.AppUserDomain.Entities;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.TenantDomain.Consts;
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
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AppRole> AppRoles => Set<AppRole>();
    public DbSet<AppUserRole> AppUserRoles => Set<AppUserRole>();
    public DbSet<AppUserClaim> AppUserClaims => Set<AppUserClaim>();
    public DbSet<AppRoleClaim> AppRoleClaims => Set<AppRoleClaim>();
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

        builder.Entity<ProductType>(b =>
        {
            b.ToTable("product_types");

            b.HasKey(x => x.Id);

            b.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(64);

            b.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(128);

            b.HasIndex(x => x.Code)
                .IsUnique();
        });

        builder.Entity<Holding>(b =>
        {
            b.ToTable("holdings");

            b.HasKey(x => x.Id);

            b.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(256);
        });

        builder.Entity<ClientNew>(b =>
        {
            b.ToTable("clients");

            b.HasKey(x => x.Id);

            b.Property(x => x.HoldingId)
                .IsRequired();

            b.Property(x => x.Domain)
                .IsRequired()
                .HasMaxLength(256);

            b.Property(x => x.DisplayName)
                .IsRequired()
                .HasMaxLength(256);

            b.HasOne(x => x.Holding)
                .WithMany()
                .HasForeignKey(x => x.HoldingId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.Domain)
                .IsUnique();
        });

        builder.Entity<ProductSubscription>(b =>
        {
            b.ToTable("product_subscriptions");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.HoldingId).IsRequired();
            b.Property(x => x.ClientId).IsRequired();
            b.Property(x => x.ProductTypeId).IsRequired();

            b.Property(x => x.IsActive).IsRequired();

            b.Property(x => x.ValidFrom).IsRequired();

            b.Property(x => x.ValidTo);

            b.Property(x => x.SettingsJson)
                .IsRequired()
                .HasColumnType("jsonb");

            b.HasOne(x => x.Holding)
                .WithMany()
                .HasForeignKey(x => x.HoldingId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.ProductType)
                .WithMany()
                .HasForeignKey(x => x.ProductTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new
            {
                x.TenantId,
                x.ClientId,
                x.ProductTypeId,
                x.IsActive
            });
        });

        builder.Entity<AppContent>(b =>
        {
            b.ToTable("app_contents");

            b.HasKey(x => x.Id);

            b.Property(x => x.ClientId).IsRequired();
            b.Property(x => x.ProductTypeId).IsRequired();

            b.Property(x => x.SlugKey)
                .IsRequired()
                .HasMaxLength(AppContentConsts.SlugKeyMaxLength);

            b.Property(x => x.OperationStatus).IsRequired();

            b.Property(x => x.OperationStatusDescription)
                .HasMaxLength(1024);

            b.Property(x => x.StorageVideoUrl)
                .HasMaxLength(2048);

            b.Property(x => x.CorrelationId)
                .HasMaxLength(128);

            b.HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.ProductType)
                .WithMany()
                .HasForeignKey(x => x.ProductTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new
            {
                x.ClientId,
                x.ProductTypeId
            });

            b.HasIndex(x => new
            {
                x.ClientId,
                x.ProductTypeId,
                x.SlugKey
            }).IsUnique();
        });

        builder.Entity<UserAccessGrant>(b =>
        {
            b.ToTable("user_access_grants");

            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId)
                .IsRequired();

            b.Property(x => x.UserId)
                .IsRequired();

            b.Property(x => x.ProductSubscriptionId)
                .IsRequired();

            b.Property(x => x.IsActive)
                .IsRequired();

            b.HasOne(x => x.ProductSubscription)
                .WithMany()
                .HasForeignKey(x => x.ProductSubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new
            {
                x.TenantId,
                x.UserId,
                x.ProductSubscriptionId
            }).IsUnique();

            b.HasIndex(x => new
            {
                x.TenantId,
                x.UserId,
                x.IsActive
            });
        });






        builder.ConfigureAppUserEntity();
        builder.ConfigureAppRoleEntity();
        builder.ConfigureAppUserRoleEntity();
        builder.ConfigureAppUserClaimEntity();
        builder.ConfigureAppRoleClaimEntity();
        builder.ConfigureAuthRefreshTokenEntity();
        builder.ConfigureAuthLoginAuditEntity();
        builder.ConfigureAuthPasswordPolicyEntity();
        builder.ConfigureAuthEmailConfirmationTokenEntity();
        builder.ConfigureAuthTokenRevocationEntity();
    }
}