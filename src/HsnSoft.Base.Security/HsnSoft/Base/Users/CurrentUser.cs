using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Json;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Security.Claims;
using HsnSoft.Base.Subscribe;

namespace HsnSoft.Base.Users;

public sealed class CurrentUser(ICurrentPrincipalAccessor principalAccessor) : ICurrentUser, ITransientDependency
{
    private static readonly Claim[] s_emptyClaimsArray = [];

    public bool IsAuthenticated => Id.HasValue;

    public Guid? Id => principalAccessor.Principal?.FindUserId();

    public string UserName => FindClaim(BaseClaimTypes.UserName)?.Value;

    public string Name => FindClaim(BaseClaimTypes.Name)?.Value;
    public string SurName => FindClaim(BaseClaimTypes.SurName)?.Value;

    public string PhoneNumber => FindClaim(BaseClaimTypes.PhoneNumber)?.Value;

    public bool PhoneNumberVerified => string.Equals(FindClaim(BaseClaimTypes.PhoneNumberVerified)?.Value, "true", StringComparison.InvariantCultureIgnoreCase);

    public string Email => FindClaim(BaseClaimTypes.Email)?.Value;

    public bool EmailVerified => string.Equals(FindClaim(BaseClaimTypes.EmailVerified)?.Value, "true", StringComparison.InvariantCultureIgnoreCase);

    public string SecurityStamp => FindClaim(BaseClaimTypes.SecurityStamp)?.Value;


    public Guid? TenantId => principalAccessor.Principal?.FindTenantId();
    public string TenantNormalized => FindClaim(BaseClaimTypes.TenantNormalized)?.Value;
    public bool IsSystemTenant => string.Equals(FindClaim(BaseClaimTypes.IsSystemTenant)?.Value, "true", StringComparison.InvariantCultureIgnoreCase);
    public List<Guid> AllowedTenantIds => principalAccessor?.Principal?.FindAllowedTenantIds() ?? [];
    public List<Guid> AllowedCustomerIds => principalAccessor?.Principal?.FindAllowedCustomerIds() ?? [];
    public List<string> AllowedScopeKeys => principalAccessor?.Principal?.FindAllowedScopeKeys() ?? [];


    public string[] RoleKeys => FindClaims(BaseClaimTypes.Role).Select(c => c.Value).Distinct().ToArray();

    public Claim FindClaim(string claimType)
    {
        return principalAccessor.Principal?.Claims.FirstOrDefault(c => c.Type == claimType);
    }

    public Claim[] FindClaims(string claimType)
    {
        return principalAccessor.Principal?.Claims.Where(c => c.Type == claimType).ToArray() ?? s_emptyClaimsArray;
    }

    public Claim[] GetAllClaims()
    {
        return principalAccessor.Principal?.Claims.ToArray() ?? s_emptyClaimsArray;
    }

    public bool IsInRole(string roleKey)
    {
        return FindClaims(BaseClaimTypes.Role).Any(c => c.Value == roleKey);
    }
}