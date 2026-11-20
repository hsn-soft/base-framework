using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HsnSoft.Base.EntityFrameworkCore;

/// <summary>
/// What a <see cref="DbUpdateConcurrencyException"/> means to a caller of a repository (<see cref="RepositoryException"/>): the row is not there any more (<see cref="RepositoryFailure.NotFound"/>, 404) or it is there but is not the one the caller read
/// (<see cref="RepositoryFailure.ConcurrencyConflict"/>, 409). EF says only "expected one row"; the database is asked once for each entry, and only on this path, so a write that succeeds costs nothing more.
/// </summary>
internal static class ConcurrencyFailures
{
    public static async Task<RepositoryException> OfAsync(DbUpdateConcurrencyException exception, CancellationToken cancellationToken)
    {
        List<EntityEntry> missing = [];
        foreach (EntityEntry entry in exception.Entries)
        {
            if (IsGone(entry, await entry.GetDatabaseValuesAsync(cancellationToken).ConfigureAwait(false)))
            {
                missing.Add(entry);
            }
        }

        return Build(exception, missing);
    }

    public static RepositoryException Of(DbUpdateConcurrencyException exception)
        => Build(exception, [.. exception.Entries.Where(entry => IsGone(entry, entry.GetDatabaseValues()))]);

    // a row that is not there, or that was soft deleted (the caller cannot see it any more): gone
    private static bool IsGone(EntityEntry entry, PropertyValues? stored)
        => stored is null || (typeof(ISoftDelete).IsAssignableFrom(entry.Metadata.ClrType) && stored[nameof(ISoftDelete.IsDeleted)] is true);

    // a record that is not there is the more exact answer: if some are missing, they are the ones named
    private static RepositoryException Build(DbUpdateConcurrencyException exception, List<EntityEntry> missing)
    {
        string entityName = exception.Entries.Count > 0 ? exception.Entries[0].Metadata.ClrType.Name : "The record";
        return missing.Count > 0
            ? RepositoryException.NotFound(entityName, missing.Select(KeyOf))
            : RepositoryException.Conflict(entityName, exception.Entries.Select(KeyOf));
    }

    private static string KeyOf(EntityEntry entry)
        => string.Join('-', entry.Metadata.FindPrimaryKey()?.Properties.Select(property => Convert.ToString(entry.Property(property.Name).CurrentValue, CultureInfo.InvariantCulture)) ?? []);
}
