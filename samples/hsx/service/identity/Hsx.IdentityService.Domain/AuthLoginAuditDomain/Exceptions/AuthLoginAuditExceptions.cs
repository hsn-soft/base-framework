using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.AuthLoginAuditDomain.Exceptions;

/// <summary>A sign-in audit is not there (or the caller may not see it): answered as 404.</summary>
public static class AuthLoginAuditNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"A sign-in audit {id} was not found.").WithKey("auth_login_audit_not_found", id);
}
