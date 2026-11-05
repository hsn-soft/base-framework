using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Consts;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Configurations;

public static class AuthLoginAuditConfiguration
{
    public static void ConfigureAuthLoginAudit(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AuthLoginAudit>(entity =>
        {
            entity.ToTable(EfCoreDbProperties.DbTablePrefix + AuthLoginAuditConsts.TableName, EfCoreDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.UserNameOrEmail).HasMaxLength(AuthLoginAuditConsts.UserNameOrEmailMaxLength).IsRequired();
            entity.Property(e => e.FailureReason).HasMaxLength(AuthLoginAuditConsts.ReasonMaxLength);
            entity.Property(e => e.IpAddress).HasMaxLength(AuthLoginAuditConsts.IpAddressMaxLength);
            entity.Property(e => e.UserAgent).HasMaxLength(AuthLoginAuditConsts.UserAgentMaxLength);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
