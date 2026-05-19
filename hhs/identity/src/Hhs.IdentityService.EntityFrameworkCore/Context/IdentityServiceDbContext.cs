using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hhs.IdentityService.EntityFrameworkCore.Context;

public sealed class IdentityServiceDbContext : BaseEfCoreDbContext<IdentityServiceDbContext>
{
    public IdentityServiceDbContext(IServiceProvider provider, DbContextOptions<IdentityServiceDbContext> options) : base(options, provider)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}