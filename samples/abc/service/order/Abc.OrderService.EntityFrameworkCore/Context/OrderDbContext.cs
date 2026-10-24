using Microsoft.EntityFrameworkCore;

namespace Abc.OrderService.EntityFrameworkCore.Context;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // the mappings of each domain are applied here, one `Configure<Domain>()` of Configurations/ each (as in Abc.IdentityService.EntityFrameworkCore)
    }
}
