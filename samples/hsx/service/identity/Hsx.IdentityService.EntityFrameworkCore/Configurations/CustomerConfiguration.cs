using Hsx.IdentityService.Domain.CompanyDomain.Entities;
using Hsx.IdentityService.Domain.CustomerDomain.Consts;
using Hsx.IdentityService.Domain.CustomerDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.IdentityService.EntityFrameworkCore.Configurations;

public static class CustomerConfiguration
{
    public static void ConfigureCustomer(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable(EfCoreDbProperties.DbTablePrefix + CustomerConsts.TableName, EfCoreDbProperties.DbSchema, table =>
            {
                table.HasCheckConstraint("CK_Customer_NormalizedDomain_NotEmpty", "\"NormalizedDomain\" <> ''");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Domain).HasMaxLength(CustomerConsts.DomainMaxLength).IsRequired();
            entity.Property(e => e.NormalizedDomain).HasMaxLength(CustomerConsts.DomainMaxLength).IsRequired();
            entity.HasIndex(e => e.NormalizedDomain).IsUnique();
            entity.HasIndex(e => e.CompanyId);
            entity.HasOne<Company>().WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
