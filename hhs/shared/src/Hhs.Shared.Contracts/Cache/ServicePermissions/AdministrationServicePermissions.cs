using HsnSoft.Base.Reflection;

namespace Hhs.Shared.Contracts.Cache.ServicePermissions;

public static class AdministrationServicePermissions
{
    private const string GroupName = "AdministrationService.";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(AdministrationServicePermissions));
    }

    public static class RolePermissions
    {
        private const string DomainName = $"{GroupName}{nameof(RolePermissions)}";

        public const string PageView = $"{DomainName}:PageView";
        public const string Manage = $"{DomainName}:Manage";
    }
}

public static class AdministrationOperationPermissions
{
    private const string GroupName = "AdministrationOperation.";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(AdministrationOperationPermissions));
    }
}