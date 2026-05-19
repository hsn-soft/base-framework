using Hhs.IdentityService.Domain.AppRoleDomain.Consts;
using Hhs.IdentityService.Domain.AppRoleDomain.Entities;
using Hhs.IdentityService.Domain.AppUserDomain.Consts;
using Hhs.IdentityService.Domain.AppUserDomain.Entities;
using HsnSoft.Base;
using Microsoft.EntityFrameworkCore;

namespace Hhs.IdentityService.EntityFrameworkCore.Configurations;

internal static class IdentityAppConfiguration
{
    public static void ConfigureIdentityAppEntities(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<AppUser>(b =>
        {
            // Maps to the AspNetUsers table
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AppUserConsts.TableName, EfCoreDbProperties.DbSchema);

            // Primary key
            b.HasKey(u => u.Id);

            // A concurrency token for use with the optimistic concurrency checking
            b.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();

            // Limit the size of columns to use efficient database types
            b.Property(u => u.UserName).HasMaxLength(AppUserConsts.UserNameMaxLength);
            b.Property(u => u.NormalizedUserName).HasMaxLength(AppUserConsts.NormalizedUserNameMaxLength);
            b.Property(u => u.Email).HasMaxLength(AppUserConsts.EmailMaxLength);
            b.Property(u => u.NormalizedEmail).HasMaxLength(AppUserConsts.NormalizedEmailMaxLength);
            b.Property(u => u.Name).HasMaxLength(AppUserConsts.NameMaxLength);
            b.Property(u => u.Surname).HasMaxLength(AppUserConsts.SurnameMaxLength);
            b.Property(u => u.PhoneNumber).HasMaxLength(AppUserConsts.PhoneNumberMaxLength);
            b.Property(u => u.DefaultLanguage).HasMaxLength(AppUserConsts.DefaultLanguageMaxLength);
            b.Property(u => u.AvatarSuffixUrl).HasMaxLength(AppUserConsts.AvatarSuffixUrlMaxLength);
            b.Property(x => x.TenantDomain).HasMaxLength(AppUserConsts.TenantDomainMaxLength);

            b.HasIndex(u => u.TenantId).HasDatabaseName("UserTenantIndex");

            // The relationships between User and other entity types
            // Note that these relationships are configured with no navigation properties

            // Each User can have many UserClaims
            b.HasMany<AppUserClaim>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();

            // Each User can have many UserLogins
            b.HasMany<AppUserLogin>().WithOne().HasForeignKey(ul => ul.UserId).IsRequired();

            // Each User can have many UserTokens
            b.HasMany<AppUserToken>().WithOne().HasForeignKey(ut => ut.UserId).IsRequired();

            // Each User can have many entries in the UserRole join table
            b.HasMany<AppUserRole>().WithOne().HasForeignKey(ur => ur.UserId).IsRequired();
        });

        builder.Entity<AppUserClaim>(b =>
        {
            // Maps to the AspNetUserClaims table
            b.ToTable(EfCoreDbProperties.DbTablePrefix + "AppUserClaims", EfCoreDbProperties.DbSchema);

            // Primary key
            b.HasKey(uc => uc.Id);
        });

        builder.Entity<AppUserLogin>(b =>
        {
            // Maps to the AspNetUserLogins table
            b.ToTable(EfCoreDbProperties.DbTablePrefix + "AppUserLogins", EfCoreDbProperties.DbSchema);

            // Composite primary key consisting of the LoginProvider and the key to use
            // with that provider
            b.HasKey(l => new { l.LoginProvider, l.ProviderKey });

            // Limit the size of the composite key columns due to common DB restrictions
            b.Property(l => l.LoginProvider).HasMaxLength(128);
            b.Property(l => l.ProviderKey).HasMaxLength(128);
        });

        builder.Entity<AppUserToken>(b =>
        {
            // Maps to the AspNetUserTokens table
            b.ToTable(EfCoreDbProperties.DbTablePrefix + "AppUserTokens", EfCoreDbProperties.DbSchema);

            // Composite primary key consisting of the UserId, LoginProvider and Name
            b.HasKey(t => new { t.UserId, t.LoginProvider, t.Name });

            // Limit the size of the composite key columns due to common DB restrictions
            b.Property(t => t.LoginProvider);
            b.Property(t => t.Name);
        });

        builder.Entity<AppRole>(b =>
        {
            // Maps to the AspNetRoles table
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AppRoleConsts.TableName, EfCoreDbProperties.DbSchema);

            // Primary key
            b.HasKey(r => r.Id);

            // A concurrency token for use with the optimistic concurrency checking
            b.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();

            // Limit the size of columns to use efficient database types
            b.Property(u => u.Name).HasMaxLength(AppRoleConsts.NameMaxLength);
            b.Property(u => u.NormalizedName).HasMaxLength(AppRoleConsts.NormalizedNameMaxLength);
            b.Property(x => x.TenantDomain).HasMaxLength(AppRoleConsts.TenantDomainMaxLength);

            b.HasIndex(r => r.TenantId).HasDatabaseName("RoleTenantIndex");

            // The relationships between Role and other entity types
            // Note that these relationships are configured with no navigation properties

            // Each Role can have many entries in the UserRole join table
            b.HasMany<AppUserRole>().WithOne().HasForeignKey(ur => ur.RoleId).IsRequired();

            // Each Role can have many associated RoleClaims
            b.HasMany<AppRoleClaim>().WithOne().HasForeignKey(rc => rc.RoleId).IsRequired();

            // b.HasData(
            //     new AppRole(name: "system-admin", isStatic: true),
            //     new AppRole(name: "system-manager", isStatic: true, isPublic: true), //back-office user => view,create-edit,delete
            //     new AppRole(name: "tenant-manager", isStatic: true, isPublic: true), //back-office user => view,create-edit,delete
            //     new AppRole(name: "system-editor", isStatic: true, isPublic: true), //back-office user => view,create-edit
            //     new AppRole(name: "tenant-editor", isStatic: true, isPublic: true), //back-office user => view,create-edit
            //     new AppRole(name: "system-user", isStatic: true, isPublic: true), //back-office user => view
            //     new AppRole(name: "tenant-user", isStatic: true, isPublic: true), //back-office user => view
            //     new AppRole(name: "web-user", isStatic: true, isPublic: true, isDefault: true) //public ui user
            // );
        });

        builder.Entity<AppRoleClaim>(b =>
        {
            // Maps to the AspNetRoleClaims table
            b.ToTable(EfCoreDbProperties.DbTablePrefix + "AppRoleClaims", EfCoreDbProperties.DbSchema);

            // Primary key
            b.HasKey(rc => rc.Id);
        });

        builder.Entity<AppUserRole>(b =>
        {
            // Maps to the AspNetUserRoles table
            b.ToTable(EfCoreDbProperties.DbTablePrefix + "AppUserRoles", EfCoreDbProperties.DbSchema);

            // Primary key
            b.HasKey(r => new { r.UserId, r.RoleId });
        });
    }
}