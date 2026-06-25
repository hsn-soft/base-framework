using Hhs.IdentityService.Domain.AppRoleDomain.Consts;
using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using Hhs.IdentityService.Domain.AppUserDomain.Consts;
using Hhs.IdentityService.Domain.AppUserDomain.Entities;
using Hhs.IdentityService.Domain.AuthDomain.Consts;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hhs.IdentityService.EntityFrameworkCore.Configurations;

public static class AuthDomainConfiguration
{
    public static void ConfigureAppRoleEntity(this ModelBuilder builder) =>
        builder.Entity<AppRole>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AppRoleConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.Name).HasMaxLength(AppRoleConsts.NameMaxLength).IsRequired();
            b.Property(x => x.NormalizedName).HasMaxLength(AppRoleConsts.NameMaxLength).IsRequired();
            b.Property(x => x.IsStatic).IsRequired();
            b.Property(x => x.IsDefault).IsRequired();

            b.HasOne(x => x.Tenant)
                .WithMany(x => x.Roles)
                .HasForeignKey(x => x.TenantId);

            b.HasIndex(x => new { x.TenantId, x.NormalizedName })
                .IsUnique();
        });

    public static void ConfigureAppUserEntity(this ModelBuilder builder) =>
        builder.Entity<AppUser>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AppUserConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.IsStatic).IsRequired();

            b.Property(x => x.DisplayName).HasMaxLength(AppUserConsts.DisplayNameMaxLength);
            b.Property(x => x.AvatarSuffixUrl).HasMaxLength(AppUserConsts.AvatarSuffixUrlMaxLength);

            b.Property(x => x.UserName).HasMaxLength(AppUserConsts.UserNameMaxLength).IsRequired();
            b.Property(x => x.NormalizedUserName).HasMaxLength(AppUserConsts.UserNameMaxLength).IsRequired();

            b.Property(x => x.Email).HasMaxLength(AppUserConsts.EmailMaxLength).IsRequired();
            b.Property(x => x.NormalizedEmail).HasMaxLength(AppUserConsts.EmailMaxLength).IsRequired();
            b.Property(x => x.EmailConfirmed).IsRequired();

            b.Property(x => x.PhoneNumber).HasMaxLength(AppUserConsts.PhoneNumberMaxLength);
            b.Property(x => x.PhoneNumberConfirmed).IsRequired();

            b.Property(x => x.PasswordHash).HasMaxLength(AppUserConsts.PasswordHashMaxLength).IsRequired();
            b.Property(x => x.SecurityStamp).HasMaxLength(AppUserConsts.SecurityStampMaxLength).IsRequired();

            b.Property(x => x.LanguageCode).HasMaxLength(AppUserConsts.LanguageCodeMaxLength);

            b.Property(x => x.FailedLoginCount).IsRequired();

            b.Property(x => x.LockoutEndAt);
            b.Property(x => x.LastLoginAt);

            b.HasOne(x => x.Tenant)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.TenantId);

            b.HasIndex(x => new { x.TenantId, x.NormalizedUserName })
                .IsUnique();

            b.HasIndex(x => new { x.TenantId, x.NormalizedEmail })
                .IsUnique();
        });

    public static void ConfigureAppUserRoleEntity(this ModelBuilder builder) =>
        builder.Entity<AppUserRole>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AppUserRoleConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId);

            b.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId);

            b.HasIndex(x => new { x.TenantId, x.UserId, x.RoleId })
                .IsUnique();
        });

    public static void ConfigureAppUserClaimEntity(this ModelBuilder builder) =>
        builder.Entity<AppUserClaim>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AppUserClaimConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.UserId).IsRequired();
            b.Property(x => x.ClaimType).HasMaxLength(AppUserClaimConsts.ClaimTypeMaxLength).IsRequired();
            b.Property(x => x.ClaimValue).HasMaxLength(AppUserClaimConsts.ClaimValueMaxLength).IsRequired();

            b.HasOne(x => x.User)
                .WithMany(x => x.Claims)
                .HasForeignKey(x => x.UserId);

            b.HasIndex(x => new { x.TenantId, x.UserId, x.ClaimType });
        });

    public static void ConfigureAppRoleClaimEntity(this ModelBuilder builder) =>
        builder.Entity<AppRoleClaim>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AppRoleClaimConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.RoleId).IsRequired();
            b.Property(x => x.ClaimType).HasMaxLength(AppRoleClaimConsts.ClaimTypeMaxLength).IsRequired();
            b.Property(x => x.ClaimValue).HasMaxLength(AppRoleClaimConsts.ClaimValueMaxLength).IsRequired();

            b.HasOne(x => x.Role)
                .WithMany(x => x.Claims)
                .HasForeignKey(x => x.RoleId);

            b.HasIndex(x => new { x.TenantId, x.RoleId, x.ClaimType });
        });

    public static void ConfigureAppRoleSubscriptionEntity(this ModelBuilder builder) =>
        builder.Entity<AppRoleSubscription>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AppRoleSubscriptionConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.RoleId).IsRequired();
            b.Property(x => x.SubscriptionId).IsRequired();
            b.Property(x => x.IsBlocked).IsRequired();

            b.HasOne(x => x.Role)
                .WithMany(x => x.Subscriptions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Subscription)
                .WithMany()
                .HasForeignKey(x => x.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.TenantId, x.RoleId, x.SubscriptionId }).IsUnique();

            b.HasIndex(x => new { x.TenantId, x.RoleId, x.IsBlocked });
        });

    public static void ConfigureAuthRefreshTokenEntity(this ModelBuilder builder) =>
        builder.Entity<AuthRefreshToken>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AuthRefreshTokenConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.TokenHash).HasMaxLength(AuthRefreshTokenConsts.TokenHashMaxLength).IsRequired();
            b.Property(x => x.ReplacedByTokenHash).HasMaxLength(AuthRefreshTokenConsts.ReplacedByTokenHashMaxLength);

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId);

            b.HasIndex(x => x.TokenHash)
                .IsUnique();
        });

    public static void ConfigureAuthLoginAuditEntity(this ModelBuilder builder) =>
        builder.Entity<AuthLoginAudit>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AuthLoginAuditConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.TenantId);
            b.Property(x => x.UserId);
            b.Property(x => x.IsSuccess).IsRequired();

            b.Property(x => x.UserNameOrEmail).HasMaxLength(AuthLoginAuditConsts.UserNameOrEmailMaxLength).IsRequired();
            b.Property(x => x.FailureReason).HasMaxLength(AuthLoginAuditConsts.FailureReasonMaxLength);
            b.Property(x => x.IpAddress).HasMaxLength(AuthLoginAuditConsts.IpAddressMaxLength);
            b.Property(x => x.UserAgent).HasMaxLength(AuthLoginAuditConsts.UserAgentMaxLength);

            b.HasIndex(x => new { x.TenantId, x.UserId, x.CreationTime });
        });

    public static void ConfigureAuthPasswordPolicyEntity(this ModelBuilder builder) =>
        builder.Entity<AuthPasswordPolicy>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AuthPasswordPolicyConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId);

            b.HasIndex(x => x.TenantId)
                .IsUnique();
        });

    public static void ConfigureAuthEmailConfirmationTokenEntity(this ModelBuilder builder) =>
        builder.Entity<AuthEmailConfirmationToken>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AuthEmailConfirmationTokenConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.UserId).IsRequired();
            b.Property(x => x.TokenHash).HasMaxLength(AuthEmailConfirmationTokenConsts.TokenHashMaxLength).IsRequired();
            b.Property(x => x.ExpiresAt).IsRequired();
            b.Property(x => x.UsedAt);

            b.HasIndex(x => x.TokenHash)
                .IsUnique();
        });

    public static void ConfigureAuthTokenRevocationEntity(this ModelBuilder builder) =>
        builder.Entity<AuthTokenRevocation>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AuthTokenRevocationConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);

            b.Property(x => x.Jti).HasMaxLength(AuthTokenRevocationConsts.JtiMaxLength).IsRequired();
            b.Property(x => x.Reason).HasMaxLength(AuthTokenRevocationConsts.ReasonMaxLength).IsRequired();

            b.HasIndex(x => x.Jti)
                .IsUnique();
        });
}