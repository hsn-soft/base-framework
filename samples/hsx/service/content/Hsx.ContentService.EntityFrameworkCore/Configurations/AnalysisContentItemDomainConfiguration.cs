using Hsx.ContentService.Domain.AnalysisContentItemDomain.Consts;
using Hsx.ContentService.Domain.AnalysisContentItemDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.ContentService.EntityFrameworkCore.Configurations;

public static class AnalysisContentItemDomainConfiguration
{
    public static void ConfigureAnalysisContentItemDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AnalysisContentItem>(entity =>
        {
            entity.ToTable(EfCoreContentDbProperties.DbTablePrefix + AnalysisContentItemConsts.TableName, EfCoreContentDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ScopeKey).HasMaxLength(AnalysisContentItemConsts.ScopeKeyMaxLength).IsRequired();
            entity.Property(e => e.SourceTitle).HasMaxLength(AnalysisContentItemConsts.SourceTitleMaxLength);
            entity.Property(e => e.SourceDescription).HasMaxLength(AnalysisContentItemConsts.SourceDescriptionMaxLength);
            entity.Property(e => e.SourceImageUrl).HasMaxLength(AnalysisContentItemConsts.SourceImageUrlMaxLength);
            entity.HasIndex(e => e.AnalysisContentId);
            entity.HasIndex(e => e.CustomerContentId);
            entity.HasIndex(e => e.ScopeKey);
        });
    }
}
