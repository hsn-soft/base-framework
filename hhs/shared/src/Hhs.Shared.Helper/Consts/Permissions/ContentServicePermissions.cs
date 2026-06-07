using HsnSoft.Base.Reflection;

namespace Hhs.Shared.Helper.Consts.Permissions;

public static class ContentServicePermissions
{
    private const string ServiceName = "service.content";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(ContentServicePermissions));
    }

    #region clients controller

    public static class Clients
    {
        private const string ControllerName = $"{ServiceName}.clients";

        // controller actions
        public const string Read = $"{ControllerName}.read";
        public const string PagedList = $"{ControllerName}.paged-list";
        public const string Create = $"{ControllerName}.create";
        public const string Update = $"{ControllerName}.update";
        public const string Delete = $"{ControllerName}.delete";
    }

    #endregion

    #region contents controller

    public static class Contents
    {
        private const string ControllerName = $"{ServiceName}.contents";

        // controller actions
        public const string PagedList = $"{ControllerName}.paged-list";
    }

    #endregion
}

public static class ContentOperationPermissions
{
    private const string OperationName = "operation.content";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(ContentOperationPermissions));
    }
}

public static class ContentConstraintPermissions
{
    private const string ConstraintName = "constraint.content";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(ContentConstraintPermissions));
    }
}