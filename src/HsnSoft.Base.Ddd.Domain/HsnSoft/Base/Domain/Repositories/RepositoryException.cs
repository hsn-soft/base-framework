using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HsnSoft.Base.Domain.Entities;

namespace HsnSoft.Base.Domain.Repositories;

/// <summary>Why a write of a repository did not happen.</summary>
public enum RepositoryFailure
{
    /// <summary>The entity is not there: it never was, or someone else removed it after the caller read it. HTTP 404, the code <see cref="RepositoryException.NotFoundCode"/>.</summary>
    NotFound = 1,

    /// <summary>The entity is there but is no longer the one the caller read: its stamp (<see cref="IHasConcurrencyStamp"/>) is not the stored one, or the store refused the change because another change got there first. HTTP 409, the code <see cref="RepositoryException.ConflictCode"/>.</summary>
    ConcurrencyConflict = 2,

    /// <summary>The write would break a unique key or index: another record has the same value. HTTP 409, the code <see cref="RepositoryException.DuplicateCode"/>. <see cref="RepositoryException.Constraint"/> names the key or index when the store says it.</summary>
    DuplicateKey = 3,
}

/// <summary>
/// The one exception of the repositories (EF Core and MongoDB alike): a write that cannot be made because a record is not there or is not the one the caller read. It is a <see cref="BaseHttpException"/>, so the hosting layers answer it as it is (404 or 409, the text in the
/// language of the request, a stable <see cref="BaseHttpException.ErrorCode"/>) and an application service that wants another answer catches it and throws a business exception of its own. It says <b>what</b> failed (<see cref="EntityName"/>) and <b>which</b> records (<see cref="Ids"/>:
/// every one of a write of many that was not there or not current, not only the first), also in the <c>Data</c> the client reads (<c>entity</c>, <c>ids</c>), so a screen can show which row of a list failed.
/// </summary>
public sealed class RepositoryException : BaseHttpException
{
    /// <summary>The key of the text of <see cref="RepositoryFailure.NotFound"/> (<c>Repository:NotFound</c>).</summary>
    public const string NotFoundMessageKey = "Repository:NotFound";

    /// <summary>The key of the text of <see cref="RepositoryFailure.ConcurrencyConflict"/> (<c>Concurrency:Conflict</c>).</summary>
    public const string ConflictMessageKey = ConcurrencyConflict.MessageKey;

    /// <summary>The error code of <see cref="RepositoryFailure.NotFound"/>.</summary>
    public const string NotFoundCode = "entity_not_found";

    /// <summary>The error code of <see cref="RepositoryFailure.ConcurrencyConflict"/>.</summary>
    public const string ConflictCode = "concurrency_conflict";

    /// <summary>The key of the text of <see cref="RepositoryFailure.DuplicateKey"/> (<c>Repository:Duplicate</c>).</summary>
    public const string DuplicateMessageKey = "Repository:Duplicate";

    /// <summary>The error code of <see cref="RepositoryFailure.DuplicateKey"/>.</summary>
    public const string DuplicateCode = "duplicate_key";

    private RepositoryException(RepositoryFailure failure, string entityName, IReadOnlyList<string> ids, string message, string? constraint = null, Exception? inner = null)
        : base(failure == RepositoryFailure.NotFound ? 404 : 409, message, inner)
    {
        Failure = failure;
        EntityName = entityName;
        Ids = ids;
        Constraint = constraint;
        (string key, string code) = failure switch
        {
            RepositoryFailure.NotFound => (NotFoundMessageKey, NotFoundCode),
            RepositoryFailure.DuplicateKey => (DuplicateMessageKey, DuplicateCode),
            _ => (ConflictMessageKey, ConflictCode),
        };
        WithKey(key, entityName);
        WithCode(code);
        Data["entity"] = entityName;
        if (ids.Count > 0)
        {
            Data["ids"] = string.Join(", ", ids);
        }

        if (constraint is not null)
        {
            Data["constraint"] = constraint;
        }
    }

    public RepositoryFailure Failure { get; }

    /// <summary>The name of the entity type (<c>AppRole</c>).</summary>
    public string EntityName { get; }

    /// <summary>The keys of the records that failed (as the text of the key); empty when the store does not say which.</summary>
    public IReadOnlyList<string> Ids { get; }

    /// <summary>For <see cref="RepositoryFailure.DuplicateKey"/>: the name of the unique key or index that would be broken, as the store says it (<c>ux_user_email</c>); <see langword="null"/> when the store does not say. The value that is duplicated is never in it.</summary>
    public string? Constraint { get; }

    /// <summary>The records are not there.</summary>
    /// <param name="entityName">The name of the entity type.</param>
    /// <param name="ids">The keys that were not found.</param>
    public static RepositoryException NotFound(string entityName, IEnumerable<object?>? ids = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        return new RepositoryException(RepositoryFailure.NotFound, entityName, Texts(ids), $"{entityName} was not found: it never was, or someone else removed it after it was read.");
    }

    /// <summary>The records are there but changed (or removed) after the caller read them.</summary>
    /// <param name="entityName">The name of the entity type.</param>
    /// <param name="ids">The keys of the records that are not current.</param>
    public static RepositoryException Conflict(string entityName, IEnumerable<object?>? ids = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        return new RepositoryException(RepositoryFailure.ConcurrencyConflict, entityName, Texts(ids), $"{entityName} was changed or removed by someone else after it was read.");
    }

    /// <summary>The write would break a unique key or index.</summary>
    /// <param name="entityName">The name of the entity type.</param>
    /// <param name="constraint">The name of the key or index, when the store says it.</param>
    /// <param name="inner">What the store threw.</param>
    public static RepositoryException Duplicate(string entityName, string? constraint = null, Exception? inner = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        return new RepositoryException(RepositoryFailure.DuplicateKey, entityName, [], $"{entityName} has a unique value that another record already has.", string.IsNullOrWhiteSpace(constraint) ? null : constraint, inner);
    }

    private static string[] Texts(IEnumerable<object?>? ids)
        => ids is null ? [] : [.. ids.Where(id => id is not null).Select(id => Convert.ToString(id, CultureInfo.InvariantCulture)!).Distinct(StringComparer.Ordinal)];
}

/// <summary>What <see cref="IRepository{TEntity, TKey}.UpdateAsync"/> did.</summary>
public enum WriteOutcome
{
    /// <summary>The record was written: something in it had changed.</summary>
    Modified = 1,

    /// <summary>Nothing in the entity differs from what was read, so nothing was written (the stamp and the audit fields stay as they were). Not an error: the caller decides what it means.</summary>
    NotModified = 2,
}

/// <summary>What <see cref="IRepository{TEntity, TKey}.UpdateManyAsync"/> did: how many entities it was given and how many of them had changed (the others are <see cref="NotModified"/>).</summary>
/// <param name="Requested">How many entities were given.</param>
/// <param name="Modified">How many of them were written.</param>
public readonly record struct WriteResult(int Requested, int Modified)
{
    /// <summary>How many entities had nothing to write.</summary>
    public int NotModified => Requested - Modified;
}
