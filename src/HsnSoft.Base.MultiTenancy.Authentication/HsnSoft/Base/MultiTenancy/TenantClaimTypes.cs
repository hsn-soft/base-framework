namespace HsnSoft.Base.MultiTenancy;

/// <summary>The claim names of the tenant of a token. They are short JWT names, as <c>BaseClaimTypes</c> of <c>HsnSoft.Base.Authentication</c> are.</summary>
public static class TenantClaimTypes
{
    /// <summary>The tenant of the user (a guid).</summary>
    public const string TenantId = "tenant_id";

    /// <summary>"true" for a user of the system tenant, which sees every tenant.</summary>
    public const string IsSystemTenant = "is_system_tenant";

    /// <summary>A tenant, besides its own, that the user may see (a guid); one claim per tenant.</summary>
    public const string AllowedTenantId = "allowed_tenant_id";
}
