using Abc.IdentityService.Domain.UserDomain.Entities;
using Abc.IdentityService.EntityFrameworkCore.Configurations;
using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EventBus.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Abc.IdentityService.EntityFrameworkCore.Context;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
    : MultiTenantDbContext(options, dataFilter, currentTenant)
{
    public DbSet<User> Users => Set<User>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureUserDomain();

        // the inbox of the service: a message that comes twice is handled once
        modelBuilder.ConfigureEventInbox(EfCoreIdentityDbProperties.DbTablePrefix + "InboxMessage", EfCoreIdentityDbProperties.DbSchema);
    }
}
