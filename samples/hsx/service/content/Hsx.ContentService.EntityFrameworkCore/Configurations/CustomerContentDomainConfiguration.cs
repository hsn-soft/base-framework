using Hsx.ContentService.Domain.CustomerContentDomain.Consts;
using Hsx.ContentService.Domain.CustomerContentDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.ContentService.EntityFrameworkCore.Configurations;

public static class CustomerContentDomainConfiguration
{
    public static void ConfigureCustomerContentDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<CustomerContent>(entity =>
        {
            entity.ToTable(EfCoreContentDbProperties.DbTablePrefix + CustomerContentConsts.TableName, EfCoreContentDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ScopeKey).HasMaxLength(CustomerContentConsts.ScopeKeyMaxLength).IsRequired();
            entity.Property(e => e.ContentKey).HasMaxLength(CustomerContentConsts.ContentKeyMaxLength).IsRequired();
            entity.Property(e => e.NormalizedContentKey).HasMaxLength(CustomerContentConsts.ContentKeyMaxLength).IsRequired();
            entity.Property(e => e.SlugKey).HasMaxLength(CustomerContentConsts.SlugKeyMaxLength).IsRequired();
            entity.Property(e => e.NormalizeStatus).HasMaxLength(CustomerContentConsts.NormalizeStatusMaxLength);
            entity.Property(e => e.VideoStatus).HasMaxLength(CustomerContentConsts.VideoStatusMaxLength);
            entity.Property(e => e.VideoCdnUrl).HasMaxLength(CustomerContentConsts.VideoCdnUrlMaxLength);
            entity.Property(e => e.CurrentMilestone).HasMaxLength(CustomerContentConsts.CurrentMilestoneMaxLength);
            entity.Property(e => e.LastError).HasMaxLength(CustomerContentConsts.LastErrorMaxLength);
            entity.HasIndex(e => e.ScopeKey);
        });
    }
}
