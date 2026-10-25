using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Xyz.AuthServer.Domain.UserDomain.Entities;
using Xyz.AuthServer.Infrastructure.Configurations;

namespace Xyz.AuthServer.Infrastructure.Context;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
    : BaseDbContext(options, dataFilter, currentTenant)
{
    public DbSet<User> Users => Set<User>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.ConfigureUserDomain();
}
