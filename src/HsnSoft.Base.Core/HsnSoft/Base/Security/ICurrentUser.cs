using System;
using System.Collections.Generic;

namespace HsnSoft.Base.Security;

/// <summary>
/// Who is making the request, for the code that does not know HTTP (the persistence that writes who created a row). The host answers it from the token
/// (<c>HsnSoft.Base.Authentication</c> registers it with the JWT authentication); when no user is signed in on the request it answers from <see cref="ActingUser"/>, which a message handler or a worker sets; a test answers it with its own.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Whether a user is signed in.</summary>
    bool IsAuthenticated { get; }

    /// <summary>The id of the user (the <c>user_id</c> claim); what the audit fields keep. <see langword="null"/> when there is no signed-in user.</summary>
    Guid? Id { get; }

    /// <summary>The user name (the <c>sub</c> claim).</summary>
    string? UserName { get; }

    /// <summary>The tenant of the user (the <c>tenant_id</c> claim).</summary>
    Guid? TenantId { get; }

    /// <summary>Whether the user belongs to the system tenant, which sees every tenant (the <c>is_system_tenant</c> claim).</summary>
    bool IsSystemTenant { get; }

    /// <summary>The other tenants the user may see (the <c>allowed_tenant_id</c> claims).</summary>
    IReadOnlyList<Guid> AllowedTenantIds { get; }

    /// <summary>The roles of the user (the <c>role</c> claims).</summary>
    IReadOnlyList<string> Roles => [];
}
