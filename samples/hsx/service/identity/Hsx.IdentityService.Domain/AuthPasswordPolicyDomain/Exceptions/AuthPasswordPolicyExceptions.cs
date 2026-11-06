using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Exceptions;

/// <summary>A password policy is not there (or the caller may not see it): answered as 404.</summary>
public static class AuthPasswordPolicyNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"A password policy {id} was not found.").WithKey("auth_password_policy_not_found", id);
}

/// <summary>The password does not keep the rules of the tenant: <c>app_password_policy_violation</c>, with the rules it breaks.</summary>
public sealed class PasswordPolicyViolationException : BusinessException
{
    public const string ErrorKey = "app_password_policy_violation";

    public PasswordPolicyViolationException(IReadOnlyList<string> broken)
        : base(ErrorKey, $"The password does not keep the rules of the tenant: {string.Join(", ", broken)}.") => WithKey(ErrorKey, string.Join(", ", broken));
}
