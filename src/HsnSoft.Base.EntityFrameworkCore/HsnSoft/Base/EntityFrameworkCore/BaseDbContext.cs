using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HsnSoft.Base.EntityFrameworkCore;

/// <summary>
/// The DbContext of a service that has entities with the concepts of the framework: it puts the query filters of <see cref="ISoftDelete"/> (the deleted ones are not seen) and
/// <see cref="IMultiTenant"/> (only the tenants the caller may see) on the entities that have them, and switches them with <see cref="IDataFilter"/> and <see cref="ICurrentTenant"/>.
/// The audit fields, the tenant of a new entity and the soft delete of a deleted one are done on save by <see cref="BaseEntityInterceptor"/> (registered by the database provider).
/// Override <see cref="ConfigureModel"/> for the mappings, not <c>OnModelCreating</c>: the filters are put on after them.
/// </summary>
public abstract class BaseDbContext(DbContextOptions options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null) : DbContext(options)
{
    private static readonly MethodInfo s_configureFilter = typeof(BaseDbContext).GetMethod(nameof(ConfigureFilters), BindingFlags.Instance | BindingFlags.NonPublic)!;

    // read by the query filters (EF turns them into parameters of the query of this context)
    private bool SoftDeleteFilterEnabled => dataFilter?.IsEnabled<ISoftDelete>() ?? true;

    private bool TenantFilterEnabled => dataFilter?.IsEnabled<IMultiTenant>() ?? true;

    private bool SeesEveryTenant => currentTenant?.IsSystemTenant ?? false;

    private Guid[] VisibleTenantIds => currentTenant is null ? [] : [.. currentTenant.AllowedTenantIds];

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

    private void ConfigureFilters<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class
    {
        Expression<Func<TEntity, bool>>? filter = null;
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            filter = entity => !SoftDeleteFilterEnabled || !EF.Property<bool>(entity, nameof(ISoftDelete.IsDeleted));
        }

        if (typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity)))
        {
            Expression<Func<TEntity, bool>> tenant = entity => !TenantFilterEnabled || SeesEveryTenant || VisibleTenantIds.Contains(EF.Property<Guid>(entity, nameof(IMultiTenant.TenantId)));
            filter = filter is null ? tenant : And(filter, tenant);
        }

        if (filter is not null)
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(filter);
        }
    }

    private static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(T));
        Expression leftBody = new ParameterReplacer(left.Parameters[0], parameter).Visit(left.Body);
        Expression rightBody = new ParameterReplacer(right.Parameters[0], parameter).Visit(right.Body);
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(leftBody, rightBody), parameter);
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
    }
}
