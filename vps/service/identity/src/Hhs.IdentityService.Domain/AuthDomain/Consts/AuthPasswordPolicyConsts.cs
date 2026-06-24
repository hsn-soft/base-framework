namespace Hhs.IdentityService.Domain.AuthDomain.Consts;

public static class AuthPasswordPolicyConsts
{
    public const string TableName = "AuthPasswordPolicies";
    public const int DefaultMinLength = 8;
    public const bool DefaultRequireDigit = true;
    public const bool DefaultRequireLowercase = true;
    public const bool DefaultRequireUppercase = true;
    public const bool DefaultRequireNonAlphanumeric = false;
    public const int DefaultMaxFailedLoginCount = 5;
    public const int DefaultLockoutMinutes = 15;
}
