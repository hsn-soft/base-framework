namespace HsnSoft.Base.Security;

/// <summary>The claim names the framework reads and writes. They are the short JWT names, not the long .NET claim URIs.</summary>
public static class BaseClaimTypes
{
    /// <summary>Who the token is about (JWT "sub"): the user name.</summary>
    public const string UserId = "sub";

    /// <summary>The id of the user (a guid); what the audit fields keep.</summary>
    public const string Id = "user_id";

    /// <summary>The roles of the user; one claim per role.</summary>
    public const string Role = "role";

    /// <summary>The tenant of the user (a guid).</summary>
    public const string TenantId = "tenant_id";

    /// <summary>"true" for a user of the system tenant, which sees every tenant.</summary>
    public const string IsSystemTenant = "is_system_tenant";

    /// <summary>A tenant, besides its own, that the user may see (a guid); one claim per tenant.</summary>
    public const string AllowedTenantId = "allowed_tenant_id";
}
