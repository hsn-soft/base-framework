using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using HsnSoft.Base;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Configurations;

public static class ContentDomainConfiguration
{
    public static void ConfigureCustomerContentEntity(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<CustomerContent>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + CustomerContentConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.ScopeKey).HasMaxLength(CustomerContentConsts.ScopeKeyMaxLength).IsRequired();
            b.Property(x => x.SlugKey).HasMaxLength(CustomerContentConsts.SlugKeyMaxLength).IsRequired();
            b.Property(x => x.OperationStatus).IsRequired();
            b.Property(x => x.OperationStatusDescription).HasMaxLength(CustomerContentConsts.OperationStatusDescriptionMaxLength);
            b.Property(x => x.NormalizedRequestId);
            b.Property(x => x.ReleaseTime);
            b.Property(x => x.VideoRequestId);
            b.Property(x => x.StorageVideoUrl).HasMaxLength(CustomerContentConsts.StorageVideoUrlMaxLength);
            b.Property(x => x.CorrelationId).HasMaxLength(CustomerContentConsts.CorrelationIdMaxLength);

            b.HasIndex(x => new { x.OperationStatus });
            b.HasIndex(x => new { x.ScopeKey, x.SlugKey, x.IsDeleted })
                .IsUnique();
        });
    }

    public static void ConfigureCustomerContentVisitEntity(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<CustomerContentVisit>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + CustomerContentVisitConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.ScopeKey).HasMaxLength(CustomerContentVisitConsts.ScopeKeyMaxLength).IsRequired();
            b.Property(x => x.CustomerContentId).IsRequired();
            b.Property(x => x.VisitTime).IsRequired();
            b.Property(x => x.VisitResponse).IsRequired().HasMaxLength(CustomerContentVisitConsts.VisitResponseMaxLength);

            b.HasIndex(x => x.ScopeKey);
            b.HasIndex(x => x.CustomerContentId);
            b.HasIndex(x => x.VisitTime);
            b.HasIndex(x => x.VisitResponse);
        });
    }

    public static void ConfigureAnalysisContentEntity(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<AnalysisContent>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AnalysisContentConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(ci => ci.Id);

            b.Property(x => x.ScopeKey).HasMaxLength(AnalysisContentConsts.ScopeKeyMaxLength).IsRequired();
            b.Property(x => x.AnalysisDate).IsRequired();
            b.Property(x => x.OperationStatus).IsRequired();
            b.Property(x => x.OperationStatusDescription).HasMaxLength(AnalysisContentConsts.OperationStatusDescriptionMaxLength);
            b.Property(x => x.NormalizedRequestId);
            b.Property(x => x.VideoRequestId);
            b.Property(x => x.StorageVideoUrl).HasMaxLength(AnalysisContentConsts.StorageVideoUrlMaxLength);
            b.Property(x => x.CorrelationId).HasMaxLength(AnalysisContentConsts.CorrelationIdMaxLength);

            b.HasIndex(x => x.ScopeKey);
            b.HasIndex(x => x.AnalysisDate);
            b.HasIndex(x => x.OperationStatus);
        });
    }
}