using Abc.IdentityService.Domain.UserHistoryDomain.Entities;
using Abc.IdentityService.EntityFrameworkCore.Configurations;
using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Abc.IdentityService.EntityFrameworkCore.Context;

/// <summary>The second context of the identity service, on its own database (and, in the sample, its own provider): what happened to the users.</summary>
public sealed class HistoryDbContext(DbContextOptions<HistoryDbContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
    : BaseDbContext(options, dataFilter, currentTenant)
{
    public DbSet<UserHistory> UserHistories => Set<UserHistory>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.ConfigureUserHistoryDomain();
}
