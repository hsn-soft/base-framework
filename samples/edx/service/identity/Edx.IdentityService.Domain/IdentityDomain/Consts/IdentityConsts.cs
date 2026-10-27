namespace Edx.IdentityService.Domain.IdentityDomain.Consts;

public static class IdentityConsts
{
    public const string TenantTable = "Tenant";
    public const string UserTable = "User";
    public const string RoleTable = "Role";
    public const string UserRoleTable = "UserRole";
    public const string PermissionTable = "Permission";
    public const string RolePermissionTable = "RolePermission";
    public const string UserPermissionTable = "UserPermission";

    public const int CodeMaxLength = 64;
    public const int NameMaxLength = 128;
    public const int UserNameMaxLength = 64;
    public const int EmailMaxLength = 256;
    public const int PasswordMinLength = 6;
    public const int PasswordMaxLength = 128;
    public const int PasswordHashMaxLength = 256;
    public const int PermissionCodeMaxLength = 300;
    public const int PermissionValueMaxLength = 256;
}
