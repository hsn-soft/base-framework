namespace HsnSoft.Base.Authorization;

/// <summary>Settings of the permission store (section <c>Permissions</c>).</summary>
public sealed class PermissionOptions
{
    public const string SectionName = "Permissions";

    /// <summary>The prefix of the keys of the entries in the distributed cache, so the grants of a system have a place of their own.</summary>
    public string KeyPrefix { get; set; } = "permission";

    /// <summary>How many seconds a service keeps what it read of the store before it reads it again: a change of a grant is seen by every service after at most this long (and what was not found is kept as long). 0 reads it every time.</summary>
    public int CacheSeconds { get; set; } = 30;
}
