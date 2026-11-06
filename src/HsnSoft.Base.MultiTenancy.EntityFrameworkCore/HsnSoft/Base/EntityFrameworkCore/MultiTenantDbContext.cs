using System;
using System.Linq;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.EntityFrameworkCore;

/// <summary>
/// The <see cref="BaseDbContext"/> of a service with tenants: it adds the query filter of <see cref="IMultiTenant"/> to the entities that have it (the system tenant sees every tenant; anyone else sees its own tenant and the
/// allowed ones; without a tenant nothing of a tenant is seen) and the one of <see cref="IScopeSubscription"/> to the entities that have that (the system tenant sees every scope; anyone else sees the scope keys of its token; without
/// any nothing of a scope is seen), each switched with <see cref="IDataFilter"/>. An entity has one of them, never both (the model refuses it); an entity that has neither has no filter, so a service may have all three kinds, in different tables.
/// </summary>
public abstract class MultiTenantDbContext(DbContextOptions options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null) : BaseDbContext(options, dataFilter)
{
    /// <summary>The name of the query filter of <see cref="IMultiTenant"/>.</summary>
    public const string TenantFilterName = "MultiTenant";

    /// <summary>The name of the query filter of <see cref="IScopeSubscription"/>.</summary>
    public const string ScopeFilterName = "ScopeSubscription";

    // read by the query filter (EF turns them into parameters of the query of this context)
    private bool TenantFilterEnabled => DataFilter?.IsEnabled<IMultiTenant>() ?? true;

    private bool SeesEveryTenant => currentTenant?.IsSystemTenant ?? false;

    private Guid[] VisibleTenantIds => currentTenant is null ? [] : [.. currentTenant.AllowedTenantIds];

    private bool ScopeFilterEnabled => DataFilter?.IsEnabled<IScopeSubscription>() ?? true;

    private string[] VisibleScopeKeys => currentTenant is null ? [] : [.. currentTenant.AllowedScopeKeys];

    protected override void ConfigureEntityFilters<TEntity>(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.ConfigureEntityFilters<TEntity>(modelBuilder);
        bool ofTenant = typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity));
        bool ofScope = typeof(IScopeSubscription).IsAssignableFrom(typeof(TEntity));
        if (ofTenant && ofScope)
        {
            // two isolations on one entity would hide it from whoever does not satisfy both: a tenant owns its users and roles, a subscription owns its content, and a table has one owner
            throw new InvalidOperationException($"{typeof(TEntity).Name} is both IMultiTenant and IScopeSubscription: an entity is isolated by its tenant or by its scope, not by both.");
        }

        if (ofTenant)
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(TenantFilterName, entity => !TenantFilterEnabled || SeesEveryTenant || VisibleTenantIds.Contains(EF.Property<Guid>(entity, nameof(IMultiTenant.TenantId))));
        }

        if (ofScope)
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(ScopeFilterName, entity => !ScopeFilterEnabled || SeesEveryTenant || VisibleScopeKeys.Contains(EF.Property<string>(entity, nameof(IScopeSubscription.ScopeKey))));
        }
    }
}
