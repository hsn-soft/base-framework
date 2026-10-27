namespace HsnSoft.Base.MultiTenancy;

/// <summary>
/// Where <see cref="ICurrentTenant"/> reads the tenant from when no code changed it: the claims of the token of a request (<c>HsnSoft.Base.MultiTenancy.Authentication</c>), the envelope of a message
/// (<c>HsnSoft.Base.MultiTenancy.EventBus</c>), or a source of the host. The first source that knows a tenant wins.
/// </summary>
public interface ITenantSource
{
    /// <summary>The tenant of the caller, or <see langword="null"/> when this source does not know one.</summary>
    TenantContext? Current { get; }
}
