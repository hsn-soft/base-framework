using System;
using System.Linq;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.EntityFrameworkCore;

/// <summary>
/// The <see cref="BaseDbContext"/> of a service with tenants: it adds the query filter of <see cref="IMultiTenant"/> to the entities that have it (the system tenant sees every tenant; anyone else sees its own tenant and the
/// allowed ones; without a tenant nothing of a tenant is seen), switched with <see cref="IDataFilter"/>. The entity that is not <see cref="IMultiTenant"/> has no tenant filter, so a service may have both.
/// </summary>
public abstract class MultiTenantDbContext(DbContextOptions options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null) : BaseDbContext(options, dataFilter)
{
    /// <summary>The name of the query filter of <see cref="IMultiTenant"/>.</summary>
    public const string TenantFilterName = "MultiTenant";

    // read by the query filter (EF turns them into parameters of the query of this context)
    private bool TenantFilterEnabled => DataFilter?.IsEnabled<IMultiTenant>() ?? true;

    private bool SeesEveryTenant => currentTenant?.IsSystemTenant ?? false;

    private Guid[] VisibleTenantIds => currentTenant is null ? [] : [.. currentTenant.AllowedTenantIds];

    protected override void ConfigureEntityFilters<TEntity>(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.ConfigureEntityFilters<TEntity>(modelBuilder);
        if (typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity)))
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(TenantFilterName, entity => !TenantFilterEnabled || SeesEveryTenant || VisibleTenantIds.Contains(EF.Property<Guid>(entity, nameof(IMultiTenant.TenantId))));
        }
    }
}
