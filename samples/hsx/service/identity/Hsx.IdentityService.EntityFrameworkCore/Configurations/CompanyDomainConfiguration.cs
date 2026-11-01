using Hsx.IdentityService.Domain.CompanyDomain.Consts;
using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Configurations;

public static class CompanyDomainConfiguration
{
    public static void ConfigureCompanyDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable(EfCoreIdentityDbProperties.DbTablePrefix + CompanyConsts.TableName, EfCoreIdentityDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Title).HasMaxLength(CompanyConsts.TitleMaxLength).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(CompanyConsts.NameMaxLength).IsRequired();
            entity.Property(e => e.NormalizedName).HasMaxLength(CompanyConsts.NameMaxLength).IsRequired();
            entity.HasIndex(e => e.NormalizedName).IsUnique();
        });
    }
}
