namespace Hhs.IdentityService.Domain.AuthDomain.Consts;

public static class AuthLoginAuditConsts
{
    public const string TableName = "AuthLoginAudits";
    public const int UserNameOrEmailMaxLength = 255;
    public const int FailureReasonMaxLength = 500;
    public const int IpAddressMaxLength = 100;
    public const int UserAgentMaxLength = 500;
}
