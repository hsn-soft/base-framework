using HsnSoft.Base.Reflection;

namespace Hhs.Shared.Helper.Permissions;

public static class IdentityServicePermissions
{
    private const string ServiceName = "service.identity";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(IdentityServicePermissions));
    }

    #region tenants controller

    public static class Tenants
    {
        private const string ControllerName = $"{ServiceName}.tenants";

        // controller actions
        public const string Read = $"{ControllerName}.read";
        public const string PagedList = $"{ControllerName}.paged-list";
        public const string Create = $"{ControllerName}.create";
        public const string Update = $"{ControllerName}.update";
        public const string Delete = $"{ControllerName}.delete";
    }

    #endregion

    #region app-roles controller

    public static class AppRoles
    {
        private const string ControllerName = $"{ServiceName}.app-roles";

        // controller actions
        public const string Read = $"{ControllerName}.read";
        public const string PagedList = $"{ControllerName}.paged-list";
        public const string Create = $"{ControllerName}.create";
        public const string Update = $"{ControllerName}.update";
        public const string Delete = $"{ControllerName}.delete";
    }

    #endregion

    #region app-users controller

    public static class AppUsers
    {
        private const string ControllerName = $"{ServiceName}.app-users";

        // controller actions
        public const string Read = $"{ControllerName}.read";
        public const string PagedList = $"{ControllerName}.paged-list";
        public const string Create = $"{ControllerName}.create";
        public const string Update = $"{ControllerName}.update";
        public const string Delete = $"{ControllerName}.delete";
    }

    public static class AuditLogs
    {
        private const string ControllerName = $"{ServiceName}.app-users";

        // controller actions
        public const string PagedList = $"{ControllerName}.audit-logs.paged-list";
    }

    #endregion
}

public static class IdentityOperationPermissions
{
    private const string OperationName = "operation.identity";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(IdentityOperationPermissions));
    }

    // app-user operation
    public static class AppUsers
    {
        private const string DomainName = $"{OperationName}.app-user";

        // operation permissions
        public const string EmailUpdate = $"{DomainName}.email.update";
        public const string PhoneUpdate = $"{DomainName}.phone.update";
        public const string PhoneView = $"{DomainName}.phone.view";
        public const string IsBlockUpdate = $"{DomainName}.is-block.update";
    }
}

public static class IdentityConstraintPermissions
{
    private const string ConstraintName = "constraint.identity";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(IdentityConstraintPermissions));
    }

    // audit-logs constraints
    public static class AuditLogs
    {
        private const string DomainName = $"{ConstraintName}.audit-logs";

        // constraint permissions
        public const string MaxDays = $"{DomainName}.max-days";
    }
}