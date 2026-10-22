using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.Sample.Api.Domain.Entities;
using HsnSoft.Base.Sample.Api.EfCore.Configurations;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.Sample.Api.EfCore.Context;

public class AppEfCoreDbContext(DbContextOptions<AppEfCoreDbContext> options, IServiceProvider provider = null) : BaseEfCoreDbContext<AppEfCoreDbContext>(options, provider)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureUserEntity();
    }
}