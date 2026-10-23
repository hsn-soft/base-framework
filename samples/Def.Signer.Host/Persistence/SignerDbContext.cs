using Microsoft.EntityFrameworkCore;

namespace Def.Signer.Host.Persistence;

public sealed class SignerDbContext(DbContextOptions<SignerDbContext> options) : DbContext(options)
{
    public DbSet<SignedDocument> Documents => Set<SignedDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SignedDocument>(document =>
        {
            document.ToTable("documents");
            document.HasKey(x => x.Id);
            document.Property(x => x.Owner).HasMaxLength(100).IsRequired();
            document.Property(x => x.Title).HasMaxLength(200).IsRequired();
            document.Property(x => x.Container).HasMaxLength(63).IsRequired();
            document.Property(x => x.BlobName).HasMaxLength(300).IsRequired();
            document.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            document.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
            document.HasIndex(x => x.RequestId).IsUnique();
            document.HasIndex(x => x.Owner);
        });
    }
}
