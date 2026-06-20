using Hhs.ContentService.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.Data;

public sealed class ContentDbContext : DbContext
{
    public ContentDbContext(DbContextOptions<ContentDbContext> options) : base(options)
    {
    }

    public DbSet<CustomerContent> CustomerContents => Set<CustomerContent>();
    public DbSet<AnalysisContent> AnalysisContents => Set<AnalysisContent>();
    public DbSet<AnalysisContentItem> AnalysisContentItems => Set<AnalysisContentItem>();
    public DbSet<ContentInboxMessage> InboxMessages => Set<ContentInboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
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

            b.Property(x => x.OutlineProviderKey).HasMaxLength(100).IsRequired();
            b.Property(x => x.VideoProviderKey).HasMaxLength(100).IsRequired();
            b.Property(x => x.AudioProviderKey).HasMaxLength(100);
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

            b.Property(x => x.OutlineProviderKey).HasMaxLength(100).IsRequired();
            b.Property(x => x.VideoProviderKey).HasMaxLength(100).IsRequired();
            b.Property(x => x.AudioProviderKey).HasMaxLength(100);
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