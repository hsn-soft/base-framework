using Hhs.IdentityService.Domain.AuthDomain.Entities;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hhs.IdentityService.EntityFrameworkCore.Context;

public sealed class AuthServiceDbContext : BaseEfCoreDbContext<AuthServiceDbContext>
{
    public DbSet<AuthTenant> AuthTenants => Set<AuthTenant>();
    public DbSet<AuthUser> AuthUsers => Set<AuthUser>();
    public DbSet<AuthRole> AuthRoles => Set<AuthRole>();
    public DbSet<AuthUserRole> AuthUserRoles => Set<AuthUserRole>();
    public DbSet<AuthUserClaim> AuthUserClaims => Set<AuthUserClaim>();
    public DbSet<AuthRoleClaim> AuthRoleClaims => Set<AuthRoleClaim>();
    public DbSet<AuthRefreshToken> AuthRefreshTokens => Set<AuthRefreshToken>();
    public DbSet<AuthLoginAudit> AuthLoginAudits => Set<AuthLoginAudit>();
    public DbSet<AuthPasswordPolicy> AuthPasswordPolicies => Set<AuthPasswordPolicy>();
    public DbSet<AuthEmailConfirmationToken> AuthEmailConfirmationTokens => Set<AuthEmailConfirmationToken>();
    public DbSet<AuthTokenRevocation> AuthTokenRevocations => Set<AuthTokenRevocation>();

    public AuthServiceDbContext(IServiceProvider provider, DbContextOptions<AuthServiceDbContext> options)
        : base(options, provider)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<AuthTenant>(b =>
        {
            b.ToTable("AuthTenants");
            b.HasKey(x => x.Id);

            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.NormalizedName).HasMaxLength(100).IsRequired();
            b.Property(x => x.NormalizedAccessPath).HasMaxLength(1000).IsRequired();
            b.Property(x => x.IsSystemTenant).IsRequired();

            b.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.NormalizedName)
                .IsUnique();
            b.HasIndex(x => x.ParentId);
            b.HasIndex(x => x.NormalizedAccessPath);
            b.HasIndex(x => x.IsSystemTenant);
        });

        builder.Entity<AuthUser>(b =>
        {
            b.ToTable("AuthUsers");
            b.HasKey(x => x.Id);

            b.Property(x => x.UserName).HasMaxLength(100).IsRequired();
            b.Property(x => x.NormalizedUserName).HasMaxLength(100).IsRequired();

            b.Property(x => x.Email).HasMaxLength(255).IsRequired();
            b.Property(x => x.NormalizedEmail).HasMaxLength(255).IsRequired();

            b.Property(x => x.PasswordHash).IsRequired();
            b.Property(x => x.SecurityStamp).HasMaxLength(64).IsRequired();

            b.Property(x => x.IsStatic).IsRequired();
            b.Property(x => x.EmailConfirmed).IsRequired();
            b.Property(x => x.FailedLoginCount).IsRequired();

            b.Property(x => x.LockoutEndAt);
            b.Property(x => x.LastLoginAt);

            b.HasOne(x => x.Tenant)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.TenantId);

            b.HasIndex(x => new { x.TenantId, x.NormalizedUserName }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.NormalizedEmail }).IsUnique();
        });

        builder.Entity<AuthRole>(b =>
        {
            b.ToTable("AuthRoles");
            b.HasKey(x => x.Id);

            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.NormalizedName).HasMaxLength(100).IsRequired();
            b.Property(x => x.IsStatic).IsRequired();
            b.Property(x => x.IsDefault).IsRequired();

            b.HasOne(x => x.Tenant)
                .WithMany(x => x.Roles)
                .HasForeignKey(x => x.TenantId);

            b.HasIndex(x => new { x.TenantId, x.NormalizedName }).IsUnique();
        });

        builder.Entity<AuthUserRole>(b =>
        {
            b.ToTable("AuthUserRoles");
            b.HasKey(x => new { x.UserId, x.RoleId });

            b.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId);

            b.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId);
        });

        builder.Entity<AuthUserClaim>(b =>
        {
            b.ToTable("AuthUserClaims");
            b.HasKey(x => x.Id);

            b.Property(x => x.ClaimType).HasMaxLength(200).IsRequired();
            b.Property(x => x.ClaimValue).HasMaxLength(500).IsRequired();

            b.HasOne(x => x.User)
                .WithMany(x => x.Claims)
                .HasForeignKey(x => x.UserId);

            b.HasIndex(x => new { x.UserId, x.ClaimType });
        });

        builder.Entity<AuthRoleClaim>(b =>
        {
            b.ToTable("AuthRoleClaims");
            b.HasKey(x => x.Id);

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

            b.Property(x => x.TokenHash).HasMaxLength(500).IsRequired();

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