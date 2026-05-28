using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using HsnSoft.Base.DependencyInjection;
using HsnSoft.Base.Security.Claims;

namespace HsnSoft.Base.Users;

public class CurrentUser : ICurrentUser, ITransientDependency
{
    private static readonly Claim[] EmptyClaimsArray = new Claim[0];

    private readonly ICurrentPrincipalAccessor _principalAccessor;

    public CurrentUser(ICurrentPrincipalAccessor principalAccessor)
    {
        _principalAccessor = principalAccessor;
    }

    public virtual bool IsAuthenticated => Id.HasValue;

    public virtual Guid? Id => _principalAccessor.Principal?.FindUserId();

    public virtual string UserName => FindClaim(BaseClaimTypes.UserName)?.Value;

    public virtual string Name => FindClaim(BaseClaimTypes.Name)?.Value;
    public virtual string SurName => FindClaim(BaseClaimTypes.SurName)?.Value;

    public virtual string PhoneNumber => FindClaim(BaseClaimTypes.PhoneNumber)?.Value;

    public virtual bool PhoneNumberVerified => string.Equals(FindClaim(BaseClaimTypes.PhoneNumberVerified)?.Value, "true", StringComparison.InvariantCultureIgnoreCase);

    public virtual string Email => FindClaim(BaseClaimTypes.Email)?.Value;

    public virtual bool EmailVerified => string.Equals(FindClaim(BaseClaimTypes.EmailVerified)?.Value, "true", StringComparison.InvariantCultureIgnoreCase);

    public virtual string SecurityStamp => FindClaim(BaseClaimTypes.SecurityStamp)?.Value;


    public virtual Guid? TenantId => _principalAccessor.Principal?.FindTenantId();
    public virtual string TenantNormalized => FindClaim(BaseClaimTypes.TenantNormalized)?.Value;
    public virtual bool IsSystemTenant => string.Equals(FindClaim(BaseClaimTypes.IsSystemTenant)?.Value, "true", StringComparison.InvariantCultureIgnoreCase);
    public virtual List<Guid> AllowedTenantIds => _principalAccessor?.Principal?.FindAllowedTenantIds() ?? [];


    public virtual string[] Roles => FindClaims(BaseClaimTypes.Role).Select(c => c.Value).Distinct().ToArray();

    public virtual Claim FindClaim(string claimType)
    {
        return _principalAccessor.Principal?.Claims.FirstOrDefault(c => c.Type == claimType);
    }

    public virtual Claim[] FindClaims(string claimType)
    {
        return _principalAccessor.Principal?.Claims.Where(c => c.Type == claimType).ToArray() ?? EmptyClaimsArray;
    }

    public virtual Claim[] GetAllClaims()
    {
        return _principalAccessor.Principal?.Claims.ToArray() ?? EmptyClaimsArray;
    }

    public virtual bool IsInRole(string roleName)
    {
        return FindClaims(BaseClaimTypes.Role).Any(c => c.Value == roleName);
    }
}