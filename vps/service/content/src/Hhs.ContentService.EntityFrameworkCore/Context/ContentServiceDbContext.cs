using Hhs.ContentService.Domain.ContentDomain.Entities;
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
    public DbSet<ContentInboxMessage> InboxMessages => Set<ContentInboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        base.OnModelCreating(builder);

        builder.ConfigureCustomerVpSettingEntity();
        builder.ConfigureContentVideoGenerationLimitEntity();


                builder.Entity<CustomerContent>(b =>
        {
            b.ToTable("customer_contents");
            b.HasKey(x => x.Id);
            b.Property(x => x.ScopeKey).IsRequired().HasMaxLength(100);
            b.Property(x => x.DomainName).IsRequired().HasMaxLength(500);
            b.Property(x => x.ContentKey).IsRequired().HasMaxLength(500);
            b.Property(x => x.SlugKey).IsRequired().HasMaxLength(500);
            b.Property(x => x.NormalizeStatus).HasMaxLength(80);
            b.Property(x => x.VideoStatus).HasMaxLength(80);
            b.Property(x => x.LastFacility).HasMaxLength(120);

        });

        builder.Entity<AnalysisContent>(b =>
        {
            b.ToTable("analysis_contents");
            b.HasKey(x => x.Id);
            b.Property(x => x.ScopeKey).IsRequired().HasMaxLength(100);
            b.Property(x => x.DomainName).IsRequired().HasMaxLength(500);
            b.Property(x => x.Title).HasMaxLength(500);
            b.Property(x => x.NormalizeStatus).HasMaxLength(80);
            b.Property(x => x.VideoStatus).HasMaxLength(80);
            b.Property(x => x.LastFacility).HasMaxLength(120);

        });

        builder.Entity<AnalysisContentItem>(b =>
        {
            b.ToTable("analysis_content_items");
            b.HasKey(x => x.Id);

            b.HasIndex(x => new { x.AnalysisContentId, x.CustomerContentId }).IsUnique();
            b.HasIndex(x => new { x.AnalysisContentId, x.SortOrder }).IsUnique();

            b.HasOne(x => x.AnalysisContent)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.AnalysisContentId);

            b.HasOne(x => x.CustomerContent)
                .WithMany()
                .HasForeignKey(x => x.CustomerContentId);
        });

        builder.Entity<ContentInboxMessage>(b =>
        {
            b.ToTable("content_inbox_messages");
            b.HasKey(x => x.EventId);

            b.Property(x => x.EventName).HasMaxLength(200).IsRequired();
            b.Property(x => x.Status).HasMaxLength(50).IsRequired();
            b.Property(x => x.Payload).HasColumnType("jsonb");
        });
    }
}