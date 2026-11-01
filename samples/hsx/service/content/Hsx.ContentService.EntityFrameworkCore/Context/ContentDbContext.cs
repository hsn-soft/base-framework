using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Hsx.ContentService.Domain.AnalysisContentDomain.Entities;
using Hsx.ContentService.Domain.AnalysisContentItemDomain.Entities;
using Hsx.ContentService.Domain.CustomerContentDomain.Entities;
using Hsx.ContentService.Domain.CustomerContentVisitDomain.Entities;
using Hsx.ContentService.Domain.CustomerVpSettingDomain.Entities;
using Hsx.ContentService.EntityFrameworkCore.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Hsx.ContentService.EntityFrameworkCore.Context;

public sealed class ContentDbContext(DbContextOptions<ContentDbContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
    : MultiTenantDbContext(options, dataFilter, currentTenant)
{
    public DbSet<CustomerContent> CustomerContents => Set<CustomerContent>();

    public DbSet<AnalysisContent> AnalysisContents => Set<AnalysisContent>();

    public DbSet<AnalysisContentItem> AnalysisContentItems => Set<AnalysisContentItem>();

    public DbSet<CustomerContentVisit> CustomerContentVisits => Set<CustomerContentVisit>();

    public DbSet<CustomerVpSetting> CustomerVpSettings => Set<CustomerVpSetting>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureCustomerContentDomain();
        modelBuilder.ConfigureAnalysisContentDomain();
        modelBuilder.ConfigureAnalysisContentItemDomain();
        modelBuilder.ConfigureCustomerContentVisitDomain();
        modelBuilder.ConfigureCustomerVpSettingDomain();
    }
}
