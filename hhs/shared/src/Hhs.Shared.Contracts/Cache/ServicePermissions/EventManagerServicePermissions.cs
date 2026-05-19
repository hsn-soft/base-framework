using HsnSoft.Base.Reflection;

namespace Hhs.Shared.Contracts.Cache.ServicePermissions;

public static class EventManagerServicePermissions
{
    private const string GroupName = "EventManagerService.";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(EventManagerServicePermissions));
    }
}

public static class EventManagerOperationPermissions
{
    private const string GroupName = "EventManagerOperation.";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(EventManagerOperationPermissions));
    }
}