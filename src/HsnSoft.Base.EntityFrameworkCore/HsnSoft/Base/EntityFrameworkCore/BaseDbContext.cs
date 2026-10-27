using System;
using System.Linq;
using System.Reflection;
using HsnSoft.Base.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HsnSoft.Base.EntityFrameworkCore;

/// <summary>
/// The DbContext of a service that has entities with the concepts of the framework: it puts the query filter of <see cref="ISoftDelete"/> (the deleted ones are not seen) on the entities that have it, and
/// switches it with <see cref="IDataFilter"/>. The audit fields and the soft delete of a deleted entity are done on save by <see cref="BaseEntityInterceptor"/> (registered by the database provider).
/// A service with tenants derives from <c>MultiTenantDbContext</c> of <c>HsnSoft.Base.MultiTenancy.EntityFrameworkCore</c>, which adds the tenant filter through <see cref="ConfigureEntityFilters{TEntity}"/>.
/// Override <see cref="ConfigureModel"/> for the mappings, not <c>OnModelCreating</c>: the filters are put on after them.
/// </summary>
public abstract class BaseDbContext(DbContextOptions options, IDataFilter? dataFilter = null) : DbContext(options)
{
    /// <summary>The name of the query filter of <see cref="ISoftDelete"/> (the filters are named, so a package can add its own beside it).</summary>
    protected const string SoftDeleteFilterName = "SoftDelete";

    private static readonly MethodInfo s_configureFilter = typeof(BaseDbContext).GetMethod(nameof(ConfigureEntityFilters), BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>The switch of the filters, for the filters a derived context adds (EF turns what a filter reads from the context into parameters of the query).</summary>
    protected IDataFilter? DataFilter { get; } = dataFilter;

    // read by the query filter (EF turns it into a parameter of the query of this context)
    private bool SoftDeleteFilterEnabled => DataFilter?.IsEnabled<ISoftDelete>() ?? true;

    /// <summary>The mappings of the service.</summary>
    protected abstract void ConfigureModel(ModelBuilder modelBuilder);

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        ConfigureModel(modelBuilder);

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes().Where(type => type.BaseType is null && !type.IsOwned()))
        {
            s_configureFilter.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);
        }
    }

    /// <summary>
    /// Puts the query filters on <typeparamref name="TEntity"/> (every root entity of the model): the one of <see cref="ISoftDelete"/> here. A derived context adds its own, each under a name of its own,
    /// after calling this.
    /// </summary>
    protected virtual void ConfigureEntityFilters<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class
    {
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(SoftDeleteFilterName, entity => !SoftDeleteFilterEnabled || !EF.Property<bool>(entity, nameof(ISoftDelete.IsDeleted)));
        }
    }
}
