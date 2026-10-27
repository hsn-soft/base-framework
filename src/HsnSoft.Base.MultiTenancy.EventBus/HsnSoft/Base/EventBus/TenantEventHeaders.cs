namespace HsnSoft.Base.EventBus;

/// <summary>The headers of a message that say in which tenant it was caused (they sit beside the message on the wire, as the other headers do).</summary>
public static class TenantEventHeaders
{
    /// <summary>The tenant of the caller who started the chain (a guid).</summary>
    public const string TenantId = "TenantId";

    /// <summary>"true" when the caller belongs to the system tenant, which sees every tenant; absent otherwise.</summary>
    public const string IsSystemTenant = "IsSystemTenant";
}
