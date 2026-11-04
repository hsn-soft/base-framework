using Hsx.IdentityService.Domain.AppRoleDomain.Consts;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Consts;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Consts;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Configurations;

public static class TenantDomainConfiguration
{
    public static void ConfigureTenantDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + TenantConsts.TableName, EfCoreIdentityDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.TenantType).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(TenantConsts.TitleMaxLength).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(TenantConsts.NameMaxLength).IsRequired();
            entity.Property(e => e.NormalizedName).HasMaxLength(TenantConsts.NameMaxLength).IsRequired();
            entity.Property(e => e.AccessPath).HasMaxLength(TenantConsts.AccessPathMaxLength).IsRequired();
            entity.HasIndex(e => e.NormalizedName).IsUnique();
            entity.HasIndex(e => e.AccessPath);
            entity.HasIndex(e => e.ParentId);
        });
    }
}

public static class AppUserDomainConfiguration
{
    public static void ConfigureAppUserDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + AppUserConsts.TableName, EfCoreIdentityDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.UserName).HasMaxLength(AppUserConsts.UserNameMaxLength).IsRequired();
            entity.Property(e => e.NormalizedUserName).HasMaxLength(AppUserConsts.UserNameMaxLength).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(AppUserConsts.EmailMaxLength).IsRequired();
            entity.Property(e => e.NormalizedEmail).HasMaxLength(AppUserConsts.EmailMaxLength).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(AppUserConsts.DisplayNameMaxLength).IsRequired();
            entity.Property(e => e.LanguageCode).HasMaxLength(AppUserConsts.LanguageCodeMaxLength);
            entity.Property(e => e.PasswordHash).HasMaxLength(AppUserConsts.PasswordHashMaxLength).IsRequired();
            entity.HasIndex(e => e.NormalizedEmail).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.NormalizedUserName }).IsUnique();
        });
    }
}

public static class AppRoleDomainConfiguration
{
    public static void ConfigureAppRoleDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AppRole>(entity =>
        {
            entity.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + AppRoleConsts.TableName, EfCoreIdentityDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(AppRoleConsts.NameMaxLength).IsRequired();
            entity.Property(e => e.NormalizedName).HasMaxLength(AppRoleConsts.NameMaxLength).IsRequired();
            entity.Property(e => e.RoleType).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.NormalizedName }).IsUnique();
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.CustomerId);
        });
    }
}

public static class AppUserRoleDomainConfiguration
{
    public static void ConfigureAppUserRoleDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AppUserRole>(entity =>
        {
            entity.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + "AppUserRole", EfCoreIdentityDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
            entity.HasIndex(e => e.RoleId);
        });
    }
}

public static class AppRoleSubscriptionDomainConfiguration
{
    public static void ConfigureAppRoleSubscriptionDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AppRoleSubscription>(entity =>
        {
            entity.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + "AppRoleSubscription", EfCoreIdentityDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.HasIndex(e => new { e.RoleId, e.SubscriptionId }).IsUnique();
            entity.HasIndex(e => e.SubscriptionId);
        });
    }
}
