using HsnSoft.Base.Reflection;

namespace Hhs.Shared.Helper.Consts.Permissions;

public static class FeedRAdManagerServicePermissions
{
    private const string ServiceName = "service.feedr-admanager";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(FeedRAdManagerServicePermissions));
    }
}

public static class FeedRAdManagerOperationPermissions
{
    private const string OperationName = "operation.feedr-admanager";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(FeedRAdManagerOperationPermissions));
    }
}

public static class FeedRAdManagerConstraintPermissions
{
    private const string ConstraintName = "constraint.feedr-admanager";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(FeedRAdManagerConstraintPermissions));
    }
}