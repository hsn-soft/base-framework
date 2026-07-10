using HsnSoft.Base.Reflection;

namespace Hhs.Shared.Helper.Permissions;

public static class VideoGeneratorServicePermissions
{
    private const string ServiceName = "service.video-generator";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(VideoGeneratorServicePermissions));
    }
}

public static class VideoGeneratorOperationPermissions
{
    private const string OperationName = "operation.video-generator";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(VideoGeneratorOperationPermissions));
    }
}

public static class VideoGeneratorConstraintPermissions
{
    private const string ConstraintName = "constraint.video-generator";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(VideoGeneratorConstraintPermissions));
    }
}