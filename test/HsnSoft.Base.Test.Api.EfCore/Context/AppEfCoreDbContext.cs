using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.Test.Api.Domain.Entities;
using HsnSoft.Base.Test.Api.EfCore.Configurations;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.Test.Api.EfCore.Context;

public class AppEfCoreDbContext(DbContextOptions<AppEfCoreDbContext> options) : BaseEfCoreDbContext<AppEfCoreDbContext>(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureUserEntity();
    }
}