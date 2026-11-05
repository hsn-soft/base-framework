using HsnSoft.Base.Domain.Sorting;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Entities;

namespace Hsx.IdentityService.Domain.AuthLoginAuditDomain.Consts;

/// <summary>The fields a list of AuthLoginAudit entities may be sorted by and the sorting when it sends none.</summary>
public static class AuthLoginAuditSorting
{
    public const string Default = "createdAt desc";

    public static readonly SortFields<AuthLoginAudit> Fields = new SortFields<AuthLoginAudit>()
        .Add("userNameOrEmail", entity => entity.UserNameOrEmail)
        .Add("createdAt", entity => entity.CreatedAt);
}
