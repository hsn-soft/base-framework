namespace Hsx.IdentityService.Domain.AuthLoginAuditDomain.Consts;

public static class AuthLoginAuditConsts
{
    public const string TableName = "AuthLoginAudits";

    public const int UserNameOrEmailMaxLength = 256;

    public const int ReasonMaxLength = 64;

    public const int IpAddressMaxLength = 64;

    public const int UserAgentMaxLength = 512;

    /// <summary>The reasons a sign-in failed, as they are written to the audit (the caller is only told it failed).</summary>
    public static class Reasons
    {
        public const string UnknownUser = "unknown-user";

        public const string WrongPassword = "wrong-password";

        public const string LockedOut = "locked-out";

        public const string NotActive = "not-active";
    }
}
