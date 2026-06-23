using HsnSoft.Base.Reflection;

namespace Hhs.Shared.Helper.Consts.Permissions;

public static class EventManagerServicePermissions
{
    private const string ServiceName = "service.event-manager";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(EventManagerServicePermissions));
    }
}

public static class EventManagerOperationPermissions
{
    private const string OperationName = "operation.event-manager";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(EventManagerOperationPermissions));
    }
}

public static class EventManagerConstraintPermissions
{
    private const string ConstraintName = "constraint.event-manager";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(EventManagerConstraintPermissions));
    }
}