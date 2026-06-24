namespace Hhs.IdentityService.Domain.AuthDomain.Consts;

public static class AuthRefreshTokenConsts
{
    public const string TableName = "AuthRefreshTokens";
    public const int TokenHashMaxLength = 500;
    public const int ReplacedByTokenHashMaxLength = 500;
}
