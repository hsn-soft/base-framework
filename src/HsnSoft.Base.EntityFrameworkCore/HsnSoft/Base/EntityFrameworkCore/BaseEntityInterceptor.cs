using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HsnSoft.Base.EntityFrameworkCore;

/// <summary>
/// Does the concepts of the framework when a context saves: the audit fields (<see cref="IAuditPropertySetter"/>) of an added or changed entity, and the soft delete: the deletion of an <see cref="ISoftDelete"/> entity becomes an update that marks it deleted.
/// </summary>
public sealed class BaseEntityInterceptor(IAuditPropertySetter audit) : SaveChangesInterceptor
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
        if (context is null)
        {
            return;
        }

        foreach (EntityEntry entry in context.ChangeTracker.Entries().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    audit.SetCreation(entry.Entity);
                    break;
                case EntityState.Modified:
                    audit.SetModification(entry.Entity);
                    break;
                case EntityState.Deleted when entry.Entity is ISoftDelete:
                    SoftDelete(context, entry);
                    break;
            }
        }
    }

    private void SoftDelete(DbContext context, EntityEntry entry)
    {
        // what the entity owns (its lines) is kept with it
        foreach (CollectionEntry collection in entry.Collections.Where(collection => collection.Metadata.TargetEntityType.IsOwned()))
        {
            foreach (object owned in (IEnumerable?)collection.CurrentValue ?? Array.Empty<object>())
            {
                context.Entry(owned).State = EntityState.Unchanged;
            }
        }

        foreach (ReferenceEntry reference in entry.References.Where(reference => reference.Metadata.TargetEntityType.IsOwned() && reference.CurrentValue is not null))
        {
            context.Entry(reference.CurrentValue!).State = EntityState.Unchanged;
        }

        entry.State = EntityState.Modified;
        audit.SetDeletion(entry.Entity);
    }
}
