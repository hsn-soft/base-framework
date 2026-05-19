using HsnSoft.Base.Reflection;

namespace Hhs.Shared.Contracts.Cache.ServicePermissions;

public static class IdentityServicePermissions
{
    private const string GroupName = "IdentityService.";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(IdentityServicePermissions));
    }

    public static class AppUsers
    {
        private const string DomainName = $"{GroupName}{nameof(AppUsers)}";

        public const string PageView = $"{DomainName}:PageView";
        public const string Create = $"{DomainName}:Create";
        public const string Update = $"{DomainName}:Update";
        public const string Delete = $"{DomainName}:Delete";
    }

    public static class AppRoles
    {
        private const string DomainName = $"{GroupName}{nameof(AppRoles)}";

        public const string PageView = $"{DomainName}:PageView";
        public const string Create = $"{DomainName}:Create";
        public const string Update = $"{DomainName}:Update";
        public const string Delete = $"{DomainName}:Delete";
    }

    public static class AuditLogs
    {
        private const string DomainName = $"{GroupName}{nameof(AuditLogs)}";

        public const string PageView = $"{DomainName}:PageView";
    }
}

public static class IdentityOperationPermissions
{
    private const string GroupName = "IdentityOperation.";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(IdentityOperationPermissions));
    }

    public static class AppUsers
    {
        private const string Update = $"{GroupName}{nameof(AppUsers)}:Update";

        public const string UpdateEmail = $"{Update}:Email";
        public const string UpdatePhone = $"{Update}:Phone";
        public const string UpdateIsBlock = $"{Update}:IsBlock";
    }
}