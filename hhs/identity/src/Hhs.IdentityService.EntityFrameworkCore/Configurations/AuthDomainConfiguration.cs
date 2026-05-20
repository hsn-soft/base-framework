using Hhs.IdentityService.Domain.AuthDomain.Consts;
using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hhs.IdentityService.EntityFrameworkCore.Configurations;

public static class AuthDomainConfiguration
{
    public static void ConfigureAppRoleEntity(this ModelBuilder builder)
    {
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

            b.HasIndex(x => new { x.TenantId, x.NormalizedName }).IsUnique();
        });
    }

    public static void ConfigureAppUserEntity(this ModelBuilder builder)
    {
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

            b.HasIndex(x => new { x.TenantId, x.NormalizedUserName }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.NormalizedEmail }).IsUnique();
        });
    }
}