using System;
using System.Collections.Generic;

namespace HsnSoft.Base.Authorization;

/// <summary>
/// What one subject (a user, a role, in a scope) was granted: the codes of the permissions, and for the ones that limit a value (a constraint, <c>constraint.intake.upload.max-bytes</c>) the value too; a permission that only
/// opens something has none.
/// </summary>
public sealed class PermissionGrants(IReadOnlyDictionary<string, string?> entries)
{
    /// <summary>Nothing granted.</summary>
    public static readonly PermissionGrants None = new(new Dictionary<string, string?>());

    /// <summary>The permission code and its value (<see langword="null"/> when it has none).</summary>
    public IReadOnlyDictionary<string, string?> Entries { get; } = entries ?? throw new ArgumentNullException(nameof(entries));

    public bool Has(string permission) => Entries.ContainsKey(permission);

    /// <summary>The value of a permission that has one.</summary>
    public string? ValueOf(string permission) => Entries.TryGetValue(permission, out string? value) ? value : null;
}
