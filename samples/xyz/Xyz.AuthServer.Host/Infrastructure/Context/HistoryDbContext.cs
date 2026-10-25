using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Xyz.AuthServer.Domain.UserHistoryDomain.Entities;
using Xyz.AuthServer.Infrastructure.Configurations;

namespace Xyz.AuthServer.Infrastructure.Context;

/// <summary>The second context of the identity service, on its own database (and, in the sample, its own provider): what happened to the users.</summary>
public sealed class HistoryDbContext(DbContextOptions<HistoryDbContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
    : BaseDbContext(options, dataFilter, currentTenant)
{
    public DbSet<UserHistory> UserHistories => Set<UserHistory>();

    protected override void ConfigureModel(ModelBuilder modelBuilder) => modelBuilder.ConfigureUserHistoryDomain();
}
