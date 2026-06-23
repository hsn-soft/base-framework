using Hhs.IdentityService.Domain.AppRoleDomain.Consts;
using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using Hhs.IdentityService.Domain.AppUserDomain.Consts;
using Hhs.IdentityService.Domain.AppUserDomain.Entities;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hhs.IdentityService.EntityFrameworkCore.Configurations;

public static class AuthDomainConfiguration
{
    extension(ModelBuilder builder)
    {
        public void ConfigureAppRoleEntity() =>
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

        public void ConfigureAppUserEntity() =>
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

        public void ConfigureAppUserRoleEntity() =>
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

                b.HasIndex(x => new { x.TenantId, x.UserId, x.RoleId })
                    .IsUnique();
            });

        public void ConfigureAppUserClaimEntity() =>
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

                b.HasIndex(x => new { x.TenantId, x.UserId, x.ClaimType });
            });

        public void ConfigureAppRoleClaimEntity() =>
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

                b.HasIndex(x => new { x.TenantId, x.RoleId, x.ClaimType });
            });

        public void ConfigureAppRoleSubscriptionEntity() =>
            builder.Entity<AppRoleSubscription>(b =>
            {
                b.ToTable("AppRoleSubscriptions");
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

        public void ConfigureAuthRefreshTokenEntity() =>
            builder.Entity<AuthRefreshToken>(b =>
            {
                b.ToTable("AuthRefreshTokens");
                b.HasKey(x => x.Id);

                b.Property(x => x.TokenHash).HasMaxLength(500).IsRequired();

                b.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId);

                b.HasIndex(x => x.TokenHash)
                    .IsUnique();
            });

        public void ConfigureAuthLoginAuditEntity() =>
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

        public void ConfigureAuthPasswordPolicyEntity() =>
            builder.Entity<AuthPasswordPolicy>(b =>
            {
                b.ToTable("AuthPasswordPolicies");
                b.HasKey(x => x.Id);

                b.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId);

                b.HasIndex(x => x.TenantId)
                    .IsUnique();
            });

        public void ConfigureAuthEmailConfirmationTokenEntity() =>
            builder.Entity<AuthEmailConfirmationToken>(b =>
            {
                b.ToTable("AuthEmailConfirmationTokens");
                b.HasKey(x => x.Id);

                b.Property(x => x.UserId).IsRequired();
                b.Property(x => x.TokenHash).HasMaxLength(500).IsRequired();
                b.Property(x => x.ExpiresAt).IsRequired();
                b.Property(x => x.UsedAt);

                b.HasIndex(x => x.TokenHash)
                    .IsUnique();
            });

        public void ConfigureAuthTokenRevocationEntity() =>
            builder.Entity<AuthTokenRevocation>(b =>
            {
                b.ToTable("AuthTokenRevocations");
                b.HasKey(x => x.Id);

                b.Property(x => x.Jti).HasMaxLength(100).IsRequired();
                b.Property(x => x.Reason).HasMaxLength(500).IsRequired();

                b.HasIndex(x => x.Jti)
                    .IsUnique();
            });
    }
}