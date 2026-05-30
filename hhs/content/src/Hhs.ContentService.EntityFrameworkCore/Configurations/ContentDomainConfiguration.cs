using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using HsnSoft.Base;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Configurations;

public static class ContentDomainConfiguration
{
    public static void ConfigureAppContentEntity(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<AppContent>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AppContentConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.ClientId).IsRequired();
            b.Property(x => x.SlugKey).IsRequired().HasMaxLength(AppContentConsts.SlugKeyMaxLength);
            b.Property(x => x.OperationStatus).IsRequired();
            b.Property(x => x.OperationStatusDescription);
            b.Property(x => x.NormalizedRequestId);
            b.Property(x => x.ReleaseTime);
            b.Property(x => x.VideoRequestId);
            b.Property(x => x.StorageVideoUrl);
            b.Property(x => x.CorrelationId);

            b.HasIndex(x => new { x.IsDeleted });
            b.HasIndex(x => new { x.TenantId });

            b.HasIndex(x => new { x.OperationStatus });
            b.HasIndex(x => new { x.IsDeleted, x.ClientId, x.SlugKey }).IsUnique();

            b.HasOne(s => s.Client)
                .WithMany()
                .HasForeignKey(s => s.ClientId).OnDelete(deleteBehavior: DeleteBehavior.Restrict) //no cascade delete
                .IsRequired();
        });
    }

    public static void ConfigureAppContentVisitEntity(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<AppContentVisit>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AppContentVisitConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.ClientId).IsRequired();
            b.Property(x => x.AppContentId).IsRequired();
            b.Property(x => x.VisitTimeLine).IsRequired();
            b.Property(x => x.VisitResponse).IsRequired().HasMaxLength(AppContentVisitConsts.VisitResponseMaxLength);

            b.HasIndex(x => new { x.ClientId, x.AppContentId });
            b.HasIndex(x => new { x.ClientId, x.VisitTimeLine });
            b.HasIndex(x => new { x.ClientId, x.VisitResponse });
        });
    }

    public static void ConfigureAnalysisContentEntity(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<AnalysisContent>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AnalysisContentConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.ClientId).IsRequired();
            b.Property(x => x.AnalysisDate).IsRequired();
            b.Property(x => x.OperationStatus).IsRequired();
            b.Property(x => x.OperationStatusDescription);
            b.Property(x => x.NormalizedAnalysisId);
            b.Property(x => x.VideoRequestId);
            b.Property(x => x.StorageVideoUrl);
            b.Property(x => x.CorrelationId);

            b.HasIndex(x => new { x.IsDeleted });
            b.HasIndex(x => new { x.TenantId });

            b.HasIndex(x => new { x.OperationStatus });
            b.HasIndex(x => new { x.ClientId });

            b.HasOne(s => s.Client)
                .WithMany()
                .HasForeignKey(s => s.ClientId).OnDelete(deleteBehavior: DeleteBehavior.Restrict) //no cascade delete
                .IsRequired();
        });
    }
}