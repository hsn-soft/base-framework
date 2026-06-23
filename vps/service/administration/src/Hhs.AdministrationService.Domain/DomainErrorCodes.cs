namespace Hhs.AdministrationService.Domain;

public static class DomainErrorCodes
{
    public const string PermissionGrantNotFound = "Error:AdministrationService:000101";
    public const string PermissionGrantDuplicate = "Error:AdministrationService:000102";
    public const string PermissionGrantFilter = "Error:AdministrationService:000103";


    public const string AppMenuNotFound = "Error:AdministrationService:000201";

    public const string PermissionNotFound = "Error:AdministrationService:000301";
    public const string AppMenuPermissionNotFound = "Error:AdministrationService:000302";
    public const string AppRolePermissionNotFound = "Error:AdministrationService:000303";
    public const string AppRolePermissionConstraintNotFound = "Error:AdministrationService:000304";
    public const string PermissionDependencyNotFound = "Error:AdministrationService:000305";
}