namespace Hhs.Shared.Helper.Consts;

public static class DefaultRoleNames
{
    public const string SystemAdmin = $"{IdentityConsts.Admin}#{DefaultDomainNames.System}";
    public const string SystemUser = $"{IdentityConsts.User}#{DefaultDomainNames.System}";
    public const string AppUser = $"registered#{DefaultDomainNames.PublicApp}";
}