using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.AuthPasswordPolicyDomain;

/// <summary>The password does not keep the rules of the tenant: <c>app_password_policy_violation</c>, with the rules it breaks.</summary>
public sealed class PasswordPolicyViolationException : BusinessException
{
    public const string Code = "app_password_policy_violation";

    public PasswordPolicyViolationException(IReadOnlyList<string> broken)
        : base(Code, $"The password does not keep the rules of the tenant: {string.Join(", ", broken)}.") => WithKey(Code, string.Join(", ", broken));
}
