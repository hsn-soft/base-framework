using HsnSoft.Base.Reflection;

namespace Hhs.Shared.Contracts.Cache.ServicePermissions;

public static class TextNormalizerServicePermissions
{
    private const string GroupName = "TextNormalizerService.";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(TextNormalizerServicePermissions));
    }
}

public static class TextNormalizerOperationPermissions
{
    private const string GroupName = "TextNormalizerOperation.";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(TextNormalizerOperationPermissions));
    }
}