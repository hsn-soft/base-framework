using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HsnSoft.Base.EntityFrameworkCore;

/// <summary>Gives an added <see cref="IMultiTenant"/> entity that has no tenant the current tenant when a context saves.</summary>
public sealed class TenantEntityInterceptor(ICurrentTenant tenant) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        Apply(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        Apply(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Apply(DbContext? context)
    {
        if (context is null || tenant.Id is not { } current)
        {
            return;
        }

        foreach (EntityEntry entry in context.ChangeTracker.Entries().Where(entry => entry.State == EntityState.Added).ToList())
        {
            if (entry.Entity is IMultiTenant { TenantId: var tenantId } && tenantId == Guid.Empty)
            {
                // the entity has no method for it: the property is written through the change tracker, whatever its setter
                entry.Property(nameof(IMultiTenant.TenantId)).CurrentValue = current;
            }
        }
    }
}
