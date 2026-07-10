using HsnSoft.Base.Reflection;

namespace Hhs.Shared.Helper.Consts.Permissions;

public static class TextNormalizerServicePermissions
{
    private const string ServiceName = "service.text-normalizer";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(TextNormalizerServicePermissions));
    }

    #region normalized-requests controller

    public static class NormalizedRequests
    {
        private const string ControllerName = $"{ServiceName}.normalized-requests";

        // controller actions
        public const string PagedList = $"{ControllerName}.paged-list";
    }

    #endregion

    #region normalized-analysis controller

    public static class NormalizedAnalysis
    {
        private const string ControllerName = $"{ServiceName}.normalized-analysis";

        // controller actions
        public const string PagedList = $"{ControllerName}.paged-list";
    }

    #endregion
}

public static class TextNormalizerOperationPermissions
{
    private const string OperationName = "operation.text-normalizer";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(TextNormalizerOperationPermissions));
    }
}

public static class TextNormalizerConstraintPermissions
{
    private const string ConstraintName = "constraint.text-normalizer";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(TextNormalizerConstraintPermissions));
    }
}