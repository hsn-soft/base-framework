using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Consts;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Configurations;

public static class AuthPasswordPolicyConfiguration
{
    public static void ConfigureAuthPasswordPolicy(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AuthPasswordPolicy>(entity =>
        {
            entity.ToTable(EfCoreDbProperties.DbTablePrefix + AuthPasswordPolicyConsts.TableName, EfCoreDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.HasIndex(e => e.TenantId).IsUnique();
        });
    }
}
