using Abc.IdentityService.Domain.UserDomain.Entities;
using Abc.IdentityService.EntityFrameworkCore.Configurations;
using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Abc.IdentityService.EntityFrameworkCore.Context;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
    : BaseDbContext(options, dataFilter, currentTenant)
{
    public DbSet<User> Users => Set<User>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.ConfigureUserDomain();
}
