using System;
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

    public virtual string UserName => this.FindClaimValue(BaseClaimTypes.UserName);

    public virtual string Name => this.FindClaimValue(BaseClaimTypes.Name);

    public virtual string SurName => this.FindClaimValue(BaseClaimTypes.SurName);

    public virtual string PhoneNumber => this.FindClaimValue(BaseClaimTypes.PhoneNumber);

    public virtual bool PhoneNumberVerified => string.Equals(this.FindClaimValue(BaseClaimTypes.PhoneNumberVerified), "true", StringComparison.InvariantCultureIgnoreCase);

    public virtual string Email => this.FindClaimValue(BaseClaimTypes.Email);

    public virtual bool EmailVerified => string.Equals(this.FindClaimValue(BaseClaimTypes.EmailVerified), "true", StringComparison.InvariantCultureIgnoreCase);

    public virtual Guid? TenantId => _principalAccessor.Principal?.FindTenantId();
    public virtual string TenantDomain => _principalAccessor.Principal?.FindTenantDomain();

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