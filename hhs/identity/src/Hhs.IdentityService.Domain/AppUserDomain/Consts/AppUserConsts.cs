namespace Hhs.IdentityService.Domain.AppUserDomain.Consts;

public static class AppUserConsts
{
    private const string DefaultSorting = "{0}Email asc";

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty);
    }

    public const string TableName = "AppUsers";
    public const int UserNameMaxLength = 256;
    public const int NormalizedUserNameMaxLength = 256;
    public const int EmailMaxLength = 256;
    public const int NormalizedEmailMaxLength = 256;
    public const int PhoneNumberMaxLength = 256;

    public const int TenantDomainMaxLength = 20;
    public const int NameMaxLength = 128;
    public const int SurnameMaxLength = 128;
    public const int DefaultLanguageMaxLength = 5;
    public const int AvatarSuffixUrlMaxLength = 256;
}