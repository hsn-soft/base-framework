using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Auditing;
using HsnSoft.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HsnSoft.Base.EntityFrameworkCore;

/// <summary>
/// Does the concepts of the framework when a context saves: the audit fields (<see cref="IAuditPropertySetter"/>) of an added or changed entity, and the soft delete: the deletion of an <see cref="ISoftDelete"/> entity becomes an update that marks it deleted. For an <see cref="IHasConcurrencyStamp"/> entity the stamp it carries when it is changed or deleted is the one the save is checked against (the one its caller saw), and a changed
/// entity gets a new one.
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

            if (entry.Entity is IHasConcurrencyStamp stamped && entry.State is EntityState.Modified or EntityState.Deleted)
            {
                ExpectStamp(entry, stamped);
            }
        }
    }

    // the stamp the entity carries is the one the caller saw: the save is refused when the stored one is another; a change then gives the entity a new stamp
    private static void ExpectStamp(EntityEntry entry, IHasConcurrencyStamp stamped)
    {
        PropertyEntry property = entry.Property(nameof(IHasConcurrencyStamp.ConcurrencyStamp));
        property.OriginalValue = property.CurrentValue;
        if (entry.State == EntityState.Modified)
        {
            stamped.SetConcurrencyStamp(ConcurrencyStamps.New());
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

        // an entity that is only ISoftDelete (a base without the deletion audit fields) has nothing that marks it, so it is marked through its property
        if (entry.Entity is ISoftDelete { IsDeleted: false } && entry.Entity is not IMarkDeleted)
        {
            entry.Property(nameof(ISoftDelete.IsDeleted)).CurrentValue = true;
        }
    }
}
