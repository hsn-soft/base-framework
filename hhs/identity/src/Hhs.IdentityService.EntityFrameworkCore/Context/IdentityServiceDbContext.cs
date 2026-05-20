using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.TenantDomain.Entities;
using Hhs.IdentityService.EntityFrameworkCore.Configurations;
using HsnSoft.Base;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hhs.IdentityService.EntityFrameworkCore.Context;

public sealed class IdentityServiceDbContext : BaseEfCoreDbContext<IdentityServiceDbContext>
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

    public IdentityServiceDbContext(IServiceProvider provider, DbContextOptions<IdentityServiceDbContext> options)
        : base(options, provider)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        base.OnModelCreating(builder);

        builder.ConfigureTenantEntity();

        builder.ConfigureAppUserEntity();
        builder.ConfigureAppRoleEntity();

        builder.Entity<AppUserRole>(b =>
        {
            b.ToTable("AppUserRoles");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId);

            b.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId);

            b.HasIndex(x => new { x.UserId, x.RoleId });
        });

        builder.Entity<AppUserClaim>(b =>
        {
            b.ToTable("AppUserClaims");
            b.HasKey(x => x.Id);

            b.Property(x => x.UserId).IsRequired();
            b.Property(x => x.ClaimType).HasMaxLength(200).IsRequired();
            b.Property(x => x.ClaimValue).HasMaxLength(500).IsRequired();

            b.HasOne(x => x.User)
                .WithMany(x => x.Claims)
                .HasForeignKey(x => x.UserId);

            b.HasIndex(x => new { x.UserId, x.ClaimType });
        });

        builder.Entity<AppRoleClaim>(b =>
        {
            b.ToTable("AppRoleClaims");
            b.HasKey(x => x.Id);

            b.Property(x => x.RoleId).IsRequired();
            b.Property(x => x.ClaimType).HasMaxLength(200).IsRequired();
            b.Property(x => x.ClaimValue).HasMaxLength(500).IsRequired();

            b.HasOne(x => x.Role)
                .WithMany(x => x.Claims)
                .HasForeignKey(x => x.RoleId);

            b.HasIndex(x => new { x.RoleId, x.ClaimType });
        });

        builder.Entity<AuthRefreshToken>(b =>
        {
            b.ToTable("AuthRefreshTokens");
            b.HasKey(x => x.Id);

            b.Property(x => x.TokenHash).HasMaxLength(500).IsRequired();

            b.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId);

            b.HasIndex(x => x.TokenHash).IsUnique();
        });

        builder.Entity<AuthLoginAudit>(b =>
        {
            b.ToTable("AuthLoginAudits");
            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId);
            b.Property(x => x.UserId);
            b.Property(x => x.IsSuccess).IsRequired();

            b.Property(x => x.UserNameOrEmail).HasMaxLength(255).IsRequired();
            b.Property(x => x.FailureReason).HasMaxLength(500);
            b.Property(x => x.IpAddress).HasMaxLength(100);
            b.Property(x => x.UserAgent).HasMaxLength(500);

            b.HasIndex(x => new { x.TenantId, x.UserId, x.CreationTime });
        });

        builder.Entity<AuthPasswordPolicy>(b =>
        {
            b.ToTable("AuthPasswordPolicies");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId);

            b.HasIndex(x => x.TenantId).IsUnique();
        });

        builder.Entity<AuthEmailConfirmationToken>(b =>
        {
            b.ToTable("AuthEmailConfirmationTokens");
            b.HasKey(x => x.Id);

            b.Property(x => x.UserId).IsRequired();
            b.Property(x => x.TokenHash).HasMaxLength(500).IsRequired();
            b.Property(x => x.ExpiresAt).IsRequired();
            b.Property(x => x.UsedAt);

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId);

            b.HasIndex(x => x.TokenHash).IsUnique();
        });

        builder.Entity<AuthTokenRevocation>(b =>
        {
            b.ToTable("AuthTokenRevocations");
            b.HasKey(x => x.Id);

            b.Property(x => x.Jti).HasMaxLength(100).IsRequired();
            b.Property(x => x.Reason).HasMaxLength(500).IsRequired();

            b.HasIndex(x => x.Jti).IsUnique();
        });
    }
}