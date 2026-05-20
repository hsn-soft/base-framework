using Hhs.IdentityService.Domain.AuthDomain.Entities;

namespace Hhs.IdentityService.Domain.AuthDomain.Consts;

public static class AppUserConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(AppUser.Email);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty, DefaultSortingField);
    }

    public const string TableName = "AppUsers";

    public const int DisplayNameMaxLength = 256;
    public const int AvatarSuffixUrlMaxLength = 256;
    public const int UserNameMaxLength = 100;
    public const int EmailMaxLength = 256;
    public const int PhoneNumberMaxLength = 20;
    public const int PasswordHashMaxLength = 512;
    public const int SecurityStampMaxLength = 64;
    public const int LanguageCodeMaxLength = 5;
}