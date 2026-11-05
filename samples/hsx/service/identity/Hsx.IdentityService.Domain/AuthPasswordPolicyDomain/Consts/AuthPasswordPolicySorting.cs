using HsnSoft.Base.Domain.Sorting;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Entities;

namespace Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Consts;

/// <summary>The fields a list of AuthPasswordPolicy entities may be sorted by and the sorting when it sends none.</summary>
public static class AuthPasswordPolicySorting
{
    public const string Default = "id asc";

    public static readonly SortFields<AuthPasswordPolicy> Fields = new SortFields<AuthPasswordPolicy>()
        .Add("id", entity => entity.Id);
}
