using Hhs.FeedRService.Domain.ReportingDomain.Consts;
using Hhs.FeedRService.Domain.ReportingDomain.Entities.PostgreSQL;
using HsnSoft.Base;
using Microsoft.EntityFrameworkCore;

namespace Hhs.FeedRService.EntityFrameworkCore.Configurations;

public static class ReportingConfiguration
{
    public static void ConfigureReportingEntities(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<AdNetwork>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AdNetworkConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);
            b.Property(x => x.NetworkCode).IsRequired().HasMaxLength(AdNetworkConsts.NetworkCodeMaxLength);
            b.Property(x => x.DisplayName).HasMaxLength(AdNetworkConsts.DisplayNameMaxLength);
            b.HasIndex(x => x.NetworkCode).IsUnique();
            b.HasMany(x => x.TopLevelAdUnits).WithOne(x => x.AdNetwork).HasForeignKey(x => x.AdNetworkId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AdUnitTopLevel>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AdUnitTopLevelConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);
            b.Property(x => x.AdUnitTopLevelCode).IsRequired().HasMaxLength(AdUnitTopLevelConsts.AdUnitCodeMaxLength);
            b.HasIndex(x => new { x.AdNetworkId, x.AdUnitTopLevelCode }).IsUnique();
            b.HasIndex(x => x.AdUnitTopLevelCode);
            b.HasMany(x => x.Clients).WithOne(x => x.AdUnitTopLevel).HasForeignKey(x => x.AdUnitTopLevelId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AdUnitClient>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + AdUnitClientConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);
            b.Property(x => x.AdUnitCode).IsRequired().HasMaxLength(AdUnitClientConsts.AdUnitCodeMaxLength);
            b.Property(x => x.ClientName).HasMaxLength(AdUnitClientConsts.ClientNameMaxLength);
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.ClientId);
            b.HasIndex(x => new { x.AdUnitTopLevelId, x.AdUnitCode }).IsUnique();
            b.HasMany(x => x.DailyReports).WithOne(x => x.AdUnitClient).HasForeignKey(x => x.AdUnitClientId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DashboardResponse>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + DashboardResponseConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);
            b.Property(x => x.ReportDate).HasColumnType("date");
            b.Property(x => x.DemandChannel).IsRequired().HasMaxLength(DashboardResponseConsts.DemandChannelMaxLength);
            b.Property(x => x.DemandSubchannelName).IsRequired().HasMaxLength(DashboardResponseConsts.DemandSubchannelNameMaxLength);
            b.Property(x => x.OrderId).IsRequired().HasMaxLength(DashboardResponseConsts.OrderIdMaxLength);
            b.Property(x => x.OrderName).IsRequired().HasMaxLength(DashboardResponseConsts.OrderNameMaxLength);
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.ClientId);
            b.HasIndex(x => x.ReportDate);
            // Composite natural key: one record per (AdUnitClient, ReportDate, DemandChannel, DemandSubchannelName, OrderId)
            b.HasIndex(x => new { x.AdUnitClientId, x.ReportDate, x.DemandChannel, x.DemandSubchannelName, x.OrderId }).IsUnique();
            b.HasIndex(x => new { x.ClientId, x.ReportDate });
        });

        builder.Entity<MongoToPostgresMapping>(b =>
        {
            b.ToTable(EfCoreDbProperties.DbTablePrefix + MongoToPostgresMappingConsts.TableName, EfCoreDbProperties.DbSchema);
            b.HasKey(x => x.Id);
            b.Property(x => x.DerivationStatus).HasConversion<string>().HasMaxLength(MongoToPostgresMappingConsts.StatusMaxLength);
            b.Property(x => x.DerivedRecordIds).HasMaxLength(MongoToPostgresMappingConsts.DerivedRecordIdsMaxLength);
            b.Property(x => x.ErrorMessage).HasMaxLength(MongoToPostgresMappingConsts.ErrorMessageMaxLength);
            b.HasIndex(x => x.MongoDbDocumentId).IsUnique();
            b.HasIndex(x => x.DerivationStatus);
        });
    }
}
