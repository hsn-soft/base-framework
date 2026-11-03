using System;

namespace HsnSoft.Base.Domain.Entities;

/// <summary>
/// An entity that is protected from a lost update: it carries a stamp that the persistence changes on every change, and a change is refused (409, <see cref="ConcurrencyConflict"/>) when the stored stamp is no longer the one the caller saw.
/// The entity keeps <see cref="ConcurrencyStamp"/> with a private setter and starts it with <see cref="ConcurrencyStamps.New"/>. The caller gives back the stamp it was shown (a DTO carries it) with
/// <see cref="SetConcurrencyStamp"/> before the entity is saved; without one, only a change made between the load and the save is found.
/// </summary>
public interface IHasConcurrencyStamp
{
    string ConcurrencyStamp { get; }

    /// <summary>Writes the stamp: the persistence gives a new one on every change, and an application service gives the one its caller saw before it saves.</summary>
    void SetConcurrencyStamp(string stamp);
}

/// <summary>Makes the stamps of <see cref="IHasConcurrencyStamp"/>.</summary>
public static class ConcurrencyStamps
{
    /// <summary>The longest a stamp is (the length of the column or the field).</summary>
    public const int MaxLength = 40;

    public static string New() => Guid.NewGuid().ToString("N");
}

/// <summary>The answer to a change of an entity that was changed (or removed) by someone else after the caller read it: 409.</summary>
public static class ConcurrencyConflict
{
    /// <summary>The key of the text the client reads (<c>Concurrency:Conflict</c>).</summary>
    public const string MessageKey = "Concurrency:Conflict";

    public static BaseHttpException For(string entityName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        BaseHttpException exception = new(409, $"{entityName} was changed or removed by someone else after it was read.");
        exception.WithKey(MessageKey, entityName);
        return exception;
    }
}
