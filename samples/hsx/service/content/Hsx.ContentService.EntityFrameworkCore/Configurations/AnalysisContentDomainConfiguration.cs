using Hsx.ContentService.Domain.AnalysisContentDomain.Consts;
using Hsx.ContentService.Domain.AnalysisContentDomain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hsx.ContentService.EntityFrameworkCore.Configurations;

public static class AnalysisContentDomainConfiguration
{
    public static void ConfigureAnalysisContentDomain(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AnalysisContent>(entity =>
        {
            entity.ToTable(EfCoreContentDbProperties.DbTablePrefix + AnalysisContentConsts.TableName, EfCoreContentDbProperties.DbSchema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ScopeKey).HasMaxLength(AnalysisContentConsts.ScopeKeyMaxLength).IsRequired();
            entity.Property(e => e.NormalizeStatus).HasMaxLength(AnalysisContentConsts.NormalizeStatusMaxLength);
            entity.Property(e => e.VideoStatus).HasMaxLength(AnalysisContentConsts.VideoStatusMaxLength);
            entity.Property(e => e.VideoCdnUrl).HasMaxLength(AnalysisContentConsts.VideoCdnUrlMaxLength);
            entity.Property(e => e.CurrentMilestone).HasMaxLength(AnalysisContentConsts.CurrentMilestoneMaxLength);
            entity.Property(e => e.LastError).HasMaxLength(AnalysisContentConsts.LastErrorMaxLength);
            entity.HasIndex(e => e.ScopeKey);
        });
    }
}
