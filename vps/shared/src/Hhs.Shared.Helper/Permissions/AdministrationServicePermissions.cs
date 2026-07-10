using HsnSoft.Base.Reflection;

namespace Hhs.Shared.Helper.Permissions;

public static class AdministrationServicePermissions
{
    private const string ServiceName = "service.administration";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(AdministrationServicePermissions));
    }

    // role-permissions controller
    public static class RolePermissions
    {
        private const string ControllerName = $"{ServiceName}.role-permissions";

        // controller actions
        public const string Read = $"{ControllerName}.read";
        public const string Update = $"{ControllerName}.update";
    }

    // app-menus controller
    public static class AppMenus
    {
        private const string ControllerName = $"{ServiceName}.app-menus";

        // controller actions
        public const string Read = $"{ControllerName}.read";
        public const string PagedList = $"{ControllerName}.paged-list";
        public const string Create = $"{ControllerName}.create";
        public const string Update = $"{ControllerName}.update";
        public const string Delete = $"{ControllerName}.delete";
    }
}

public static class AdministrationOperationPermissions
{
    private const string OperationName = "operation.administration";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(AdministrationOperationPermissions));
    }
}

public static class AdministrationConstraintPermissions
{
    private const string ConstraintName = "constraint.administration";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(AdministrationConstraintPermissions));
    }
}