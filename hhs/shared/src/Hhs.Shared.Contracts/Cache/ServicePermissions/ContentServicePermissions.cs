using HsnSoft.Base.Reflection;

namespace Hhs.Shared.Contracts.Cache.ServicePermissions;

public static class ContentServicePermissions
{
    private const string GroupName = "ContentService.";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(ContentServicePermissions));
    }

    public static class Dashboards
    {
        private const string DomainName = $"{GroupName}{nameof(Dashboards)}";

        public const string ResponseStatisticView = $"{DomainName}:ResponseStatisticView";
    }

    public static class Clients
    {
        private const string DomainName = $"{GroupName}{nameof(Clients)}";

        public const string PageView = $"{DomainName}:PageView";
        public const string Create = $"{DomainName}:Create";
        public const string Update = $"{DomainName}:Update";
        public const string Delete = $"{DomainName}:Delete";
    }
}


public static class ContentOperationPermissions
{
    private const string GroupName = "ContentOperation.";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(ContentOperationPermissions));
    }
}