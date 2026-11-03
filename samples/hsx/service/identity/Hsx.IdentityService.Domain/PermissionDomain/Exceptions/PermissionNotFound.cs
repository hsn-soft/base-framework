using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.PermissionDomain.Exceptions;

/// <summary>There is no such permission, or it is not active any more: answered as 404.</summary>
public static class PermissionNotFound
{
    public static NotFoundException For(string code) => new NotFoundException($"Permission '{code}' was not found.").WithKey("permission_not_found", code);
}
