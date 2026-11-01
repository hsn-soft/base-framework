using System;
using System.Collections.Generic;
using System.Linq;

namespace HsnSoft.Base.MultiTenancy;

/// <summary>
/// The place of a tenant in the tree of tenants, written as the ids of the tenants from the root down to it, each in 32 small hex digits (<c>Guid.ToString("N")</c>) and each followed by a <c>/</c>:
/// <c>/{D}/{C}/{B}/</c> is the tenant B whose parent is C whose parent is D (the root). The path of a tenant is its parent's path and its own id, so it is made once and every tenant under a tenant has a path that starts with the path of the tenant:
/// the tenants that a tenant may see (itself and the ones under it) are the ones whose path starts with its own, found with one indexed range query and no recursion.
/// <para>Two things make it safe where a path of names (<c>/A/CC</c>) is not: the ids never change, so a tenant that is renamed keeps its path; and every id ends with <c>/</c>, so the path of <c>/A/CC/</c> does not start the path of <c>/A/CCD/</c>
/// (a path of names that is searched with <c>StartsWith("/A/CC")</c> finds the tenant <c>CCD</c> too, which is somebody else's data).</para>
/// </summary>
public static class TenantAccessPath
{
    /// <summary>The separator that ends every id.</summary>
    public const char Separator = '/';

    private const int IdLength = 32;

    /// <summary>How many characters a path of <paramref name="depth"/> tenants (the tenant itself counted) takes.</summary>
    public static int LengthFor(int depth) => 1 + (depth * (IdLength + 1));

    /// <summary>The path of a tenant that has no parent: <c>/{id}/</c>.</summary>
    public static string Root(Guid tenantId) => $"{Separator}{tenantId:N}{Separator}";

    /// <summary>The path of a tenant under another: the path of the parent and the id of the tenant.</summary>
    /// <exception cref="ArgumentException"><paramref name="parentPath"/> is not a path.</exception>
    public static string Child(string parentPath, Guid tenantId)
    {
        if (!IsValid(parentPath))
        {
            throw new ArgumentException($"'{parentPath}' is not the path of a tenant.", nameof(parentPath));
        }

        return $"{parentPath}{tenantId:N}{Separator}";
    }

    /// <summary>Whether a text is a path: it starts with <c>/</c> and is made of ids of 32 small hex digits, each followed by <c>/</c>.</summary>
    public static bool IsValid(string? path)
    {
        if (string.IsNullOrEmpty(path) || path.Length < IdLength + 2 || path[0] != Separator || path[^1] != Separator)
        {
            return false;
        }

        foreach (string segment in path[1..^1].Split(Separator))
        {
            if (segment.Length != IdLength || !Guid.TryParseExact(segment, "N", out Guid parsed) || parsed.ToString("N") != segment)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Whether the tenant at <paramref name="path"/> is the tenant at <paramref name="ancestorPath"/> or is under it.</summary>
    public static bool IsSelfOrDescendantOf(string path, string ancestorPath)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(ancestorPath);

        return path.StartsWith(ancestorPath, StringComparison.Ordinal);
    }

    /// <summary>The tenants of a path from the root down to the tenant itself.</summary>
    /// <exception cref="ArgumentException"><paramref name="path"/> is not a path.</exception>
    public static IReadOnlyList<Guid> Ids(string path)
    {
        if (!IsValid(path))
        {
            throw new ArgumentException($"'{path}' is not the path of a tenant.", nameof(path));
        }

        return [.. path[1..^1].Split(Separator).Select(segment => Guid.ParseExact(segment, "N"))];
    }

    /// <summary>The path of the parent, or null for a tenant at the root.</summary>
    /// <exception cref="ArgumentException"><paramref name="path"/> is not a path.</exception>
    public static string? Parent(string path)
    {
        IReadOnlyList<Guid> ids = Ids(path);
        return ids.Count == 1 ? null : path[..(path.Length - IdLength - 1)];
    }

    /// <summary>
    /// The path a tenant under <paramref name="oldAncestorPath"/> gets when that tenant moves to <paramref name="newAncestorPath"/> (the tenant that moves and everything under it: the start of their paths is replaced). A path that is not under it is an error.
    /// </summary>
    /// <exception cref="ArgumentException">The path is not under <paramref name="oldAncestorPath"/>, or one of them is not a path.</exception>
    public static string Move(string path, string oldAncestorPath, string newAncestorPath)
    {
        if (!IsValid(path) || !IsValid(oldAncestorPath) || !IsValid(newAncestorPath))
        {
            throw new ArgumentException("A path of a tenant is made of ids (/{id}/{id}/).");
        }

        if (!IsSelfOrDescendantOf(path, oldAncestorPath))
        {
            throw new ArgumentException($"'{path}' is not under '{oldAncestorPath}'.", nameof(path));
        }

        return newAncestorPath + path[oldAncestorPath.Length..];
    }
}
