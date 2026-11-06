namespace Hsx.IdentityService.Domain.AppUserDomain.Consts;

public static class AppUserConsts
{
    public const string TableName = "AppUsers";

    public const int UserNameMaxLength = 100;

    public const int EmailMaxLength = 256;

    public const int DisplayNameMaxLength = 256;

    public const int LanguageCodeMaxLength = 5;

    public const int PhoneNumberMaxLength = 20;
    public const int AvatarSuffixUrlMaxLength = 256;
    public const int PasswordMinLength = 8;

    public const int PasswordMaxLength = 128;

    public const int PasswordHashMaxLength = 512;
}
