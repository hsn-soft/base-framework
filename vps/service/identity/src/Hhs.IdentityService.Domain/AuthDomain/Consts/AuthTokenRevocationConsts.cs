namespace Hhs.IdentityService.Domain.AuthDomain.Consts;

public static class AuthTokenRevocationConsts
{
    public const string TableName = "AuthTokenRevocations";
    public const int JtiMaxLength = 100;
    public const int ReasonMaxLength = 500;
}
