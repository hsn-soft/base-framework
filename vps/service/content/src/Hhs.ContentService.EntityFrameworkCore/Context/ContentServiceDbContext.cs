using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.InfraDomain.Entities;
using Hhs.ContentService.Domain.SettingDomain.Entities;
using Hhs.ContentService.EntityFrameworkCore.Configurations;
using HsnSoft.Base;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.EntityFrameworkCore.Context;

public sealed class ContentServiceDbContext(
    IServiceProvider provider,
    DbContextOptions<ContentServiceDbContext> options
) : BaseEfCoreDbContext<ContentServiceDbContext>(options, provider)
{
    public DbSet<CustomerVpSetting> CustomerVpSettings => Set<CustomerVpSetting>();
    public DbSet<ContentVideoGenerationLimit> ContentVideoGenerationLimits => Set<ContentVideoGenerationLimit>();


    public DbSet<CustomerContent> CustomerContents => Set<CustomerContent>();
    public DbSet<AnalysisContent> AnalysisContents => Set<AnalysisContent>();
    public DbSet<AnalysisContentItem> AnalysisContentItems => Set<AnalysisContentItem>();
    public DbSet<EventInboxMessage> InboxMessages => Set<EventInboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        base.OnModelCreating(builder);

        // SettingDomain configuration
        builder.ConfigureCustomerVpSettingEntity();
        builder.ConfigureContentVideoGenerationLimitEntity();

        // ContentDomain configuration
        builder.ConfigureCustomerContentEntity();
        builder.ConfigureAnalysisContentEntity();
        builder.ConfigureAnalysisContentItemEntity();

        // InfraDomain configuration
        builder.ConfigureEventInboxMessageEntity();
    }
}