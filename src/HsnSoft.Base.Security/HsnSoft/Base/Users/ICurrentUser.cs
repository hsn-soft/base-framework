using System;
using System.Collections.Generic;
using System.Security.Claims;
using JetBrains.Annotations;

namespace HsnSoft.Base.Users;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    [CanBeNull] Guid? Id { get; }

    [CanBeNull] string UserName { get; }

    [CanBeNull] string Name { get; }

    [CanBeNull] string SurName { get; }

    [CanBeNull] string PhoneNumber { get; }

    bool PhoneNumberVerified { get; }

    [CanBeNull] string Email { get; }

    bool EmailVerified { get; }

    [CanBeNull] string SecurityStamp { get; }
    Guid? TenantId { get; }

    [CanBeNull] string TenantNormalized { get; }

    bool IsSystemTenant { get; }

    [NotNull] List<Guid> AllowedTenantIds { get; }

    // [NotNull] List<Guid> AllowedCustomerIds { get; }

    [NotNull] List<string> AllowedScopeKeys { get; }

    [NotNull] string[] RoleKeys { get; }

    [CanBeNull]
    Claim FindClaim(string claimType);

    [NotNull]
    Claim[] FindClaims(string claimType);

    [NotNull]
    Claim[] GetAllClaims();

    bool IsInRole(string roleKey);
}