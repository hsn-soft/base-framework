using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Consts;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Entities;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Consts;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Entities;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Configurations;

public static class AuthPasswordPolicyDomainConfiguration
{
    public static void ConfigureAuthPasswordPolicyDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AuthPasswordPolicy>(entity =>
        {
            entity.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + AuthPasswordPolicyConsts.TableName, EfCoreIdentityDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.HasIndex(e => e.TenantId).IsUnique();
        });
    }
}

public static class AuthRefreshTokenDomainConfiguration
{
    public static void ConfigureAuthRefreshTokenDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AuthRefreshToken>(entity =>
        {
            entity.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + "AuthRefreshToken", EfCoreIdentityDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(e => e.ReplacedByTokenHash).HasMaxLength(128);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.UserId);
        });
    }
}

public static class AuthLoginAuditDomainConfiguration
{
    public static void ConfigureAuthLoginAuditDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AuthLoginAudit>(entity =>
        {
            entity.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + AuthLoginAuditConsts.TableName, EfCoreIdentityDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Email).HasMaxLength(AuthLoginAuditConsts.EmailMaxLength).IsRequired();
            entity.Property(e => e.FailureReason).HasMaxLength(AuthLoginAuditConsts.ReasonMaxLength);
            entity.Property(e => e.IpAddress).HasMaxLength(AuthLoginAuditConsts.IpAddressMaxLength);
            entity.Property(e => e.UserAgent).HasMaxLength(AuthLoginAuditConsts.UserAgentMaxLength);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
