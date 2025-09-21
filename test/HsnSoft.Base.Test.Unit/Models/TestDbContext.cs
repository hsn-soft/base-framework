using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.Test.Unit.Models;

public class TestEfCoreDbContext(DbContextOptions<TestEfCoreDbContext> options) : BaseEfCoreDbContext<TestEfCoreDbContext>(options)
{
    public DbSet<TestEntity> TestEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestEntity>(b =>
        {
            b.ToTable("Tests");
            b.HasKey(ci => ci.Id);

            b.Property(x => x.Name).HasColumnName(nameof(TestEntity.Name)).IsRequired().HasMaxLength(100);

            // b.HasIndex(x => new { x.IsActive });
        });
    }
}