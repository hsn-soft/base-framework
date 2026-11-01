namespace Hsx.IdentityService.Domain.AppUserDomain.Consts;

public static class AppUserConsts
{
    public const string TableName = "AppUser";

    public const int UserNameMaxLength = 64;

    public const int EmailMaxLength = 256;

    public const int DisplayNameMaxLength = 128;

    public const int LanguageCodeMaxLength = 8;

    public const int PasswordMinLength = 8;

    public const int PasswordMaxLength = 128;

    public const int PasswordHashMaxLength = 512;
}
