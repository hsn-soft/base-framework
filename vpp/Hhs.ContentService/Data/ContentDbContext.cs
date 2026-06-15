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
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<CustomerContent>(b =>
        {
            b.ToTable("customer_contents");
            b.HasKey(x => x.Id);
            b.Property(x => x.Url).IsRequired();
            b.Property(x => x.NormalizeStatus).HasMaxLength(80);
            b.Property(x => x.VideoStatus).HasMaxLength(80);
            b.Property(x => x.LastFacility).HasMaxLength(120);
        });

        builder.Entity<AnalysisContent>(b =>
        {
            b.ToTable("analysis_contents");
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).HasMaxLength(500);
            b.Property(x => x.NormalizeStatus).HasMaxLength(80);
            b.Property(x => x.VideoStatus).HasMaxLength(80);
            b.Property(x => x.LastFacility).HasMaxLength(120);
        });

        builder.Entity<AnalysisContentItem>(b =>
        {
            b.ToTable("analysis_content_items");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.AnalysisContent)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.AnalysisContentId);

            b.HasOne(x => x.CustomerContent)
                .WithMany()
                .HasForeignKey(x => x.CustomerContentId);
        });

        builder.Entity<InboxMessage>(b =>
        {
            b.ToTable("inbox_messages");
            b.HasKey(x => x.EventId);
        });
    }
}