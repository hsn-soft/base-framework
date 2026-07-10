namespace Hhs.Shared.Helper.Constants;

public static class NameConsts
{
    public const string SolutionName = "hhs";
    public const string System = "system";
    public const string Tenant = "tenant";
    public const string Registered = "registered";
}

public static class DefaultUserNames
{
    public const string Admin = "admin";
    public const string User = "user";

    public const string SystemAdmin = $"{NameConsts.System}-{Admin}";
    public const string TenantAdmin = $"{NameConsts.Tenant}-{Admin}";

    public const string SystemUser = $"{NameConsts.System}-{User}";
}

public static class DefaultRoleNames
{
    public const string Admin = "admin";
    public const string User = "user";

    public const string SystemAdmin = $"{NameConsts.System}-{Admin}";
    public const string TenantAdmin = $"{NameConsts.Tenant}-{Admin}";

    public const string SystemUser = $"{NameConsts.System}-{User}";
    public const string RegisteredUser = $"{NameConsts.Registered}-{User}";
}

public static class TenantConsts
{
    public const string SystemTenantId = "E3BEADA6-9B33-4A32-B8E6-E16D29D78D23";
    public const string SystemTenantName = "system";

    public const string SystemTenantRoleId = "F37CD985-AA93-40B2-BCC4-7862AC3719EB";
    public const string SystemTenantUserId = "1C07F116-9284-4ACF-8188-D357CA9D0833";
}

public static class AppUiNames
{
    public const string CommercialClientId = $"{NameConsts.SolutionName}-app-commercial";
    public const string BackOfficeClientId = $"{NameConsts.SolutionName}-app-back-office";
    public const string PublicClientId = $"{NameConsts.SolutionName}-app-public";
}