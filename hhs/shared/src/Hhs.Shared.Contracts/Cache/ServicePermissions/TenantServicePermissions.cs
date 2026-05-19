using HsnSoft.Base.Reflection;

namespace Hhs.Shared.Contracts.Cache.ServicePermissions;

public static class TenantServicePermissions
{
    private const string GroupName = "TenantService.";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(TenantServicePermissions));
    }

    public static class Tenants
    {
        private const string DomainName = $"{GroupName}{nameof(Tenants)}";

        public const string PageView = $"{DomainName}:PageView";
        public const string Detail = $"{DomainName}:Detail";
        public const string Create = $"{DomainName}:Create";
        public const string Update = $"{DomainName}:Update";
        public const string Delete = $"{DomainName}:Delete";
    }
}

public static class TenantOperationPermissions
{
    private const string GroupName = "TenantOperation.";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(TenantOperationPermissions));
    }

    public static class Tenants
    {
        private const string Detail = $"{GroupName}{nameof(Tenants)}:Detail";
        private const string Update = $"{GroupName}{nameof(Tenants)}:Update";

        public const string DetailEmail = $"{Detail}:Email";
        public const string DetailPhone = $"{Detail}:Phone";

        public const string UpdateIdentityCode = $"{Update}:IdentityCode";
        public const string UpdateEmail = $"{Update}:Email";
        public const string UpdatePhone = $"{Update}:Phone";
    }
}