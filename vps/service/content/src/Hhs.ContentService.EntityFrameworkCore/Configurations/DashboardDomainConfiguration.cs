namespace Hhs.ContentService.EntityFrameworkCore.Configurations;

// public static class DashboardDomainConfiguration
// {
//     public static void ConfigureResponseStatisticEntity(this ModelBuilder builder)
//     {
//         Check.NotNull(builder, nameof(builder));
//
//         builder.Entity<ResponseStatistic>(b =>
//         {
//             b.ToTable(EfCoreDbProperties.DbTablePrefix + ResponseStatisticConsts.TableName, EfCoreDbProperties.DbSchema);
//             b.HasKey(ci => ci.Id);
//
//             b.Property(x => x.ClientId).HasColumnName(nameof(ResponseStatistic.ClientId)).IsRequired();
//             b.Property(x => x.ResponseStatus).HasColumnName(nameof(ResponseStatistic.ResponseStatus)).IsRequired().HasMaxLength(ResponseStatisticConsts.ResponseStatusMaxLength);
//             b.Property(x => x.ResponseTime).HasColumnName(nameof(ResponseStatistic.ResponseTime)).IsRequired();
//             b.Property(x => x.ResponseCount).HasColumnName(nameof(ResponseStatistic.ResponseCount)).IsRequired();
//
//
//             b.HasIndex(x => new { x.TenantId });
//
//             b.HasIndex(x => new { x.ClientId, x.ResponseStatus });
//             b.HasIndex(x => new { x.ClientId, x.ResponseTime });
//         });
//     }
// }