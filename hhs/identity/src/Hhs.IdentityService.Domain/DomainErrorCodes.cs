namespace Hhs.IdentityService.Domain;

public static class DomainErrorCodes
{
    public const string FakeNotFound = "Error:IdentityService:000311";

    public const string UnauthorizedTenantError = "Error:IdentityService:000010";
    public const string InvalidTenantDomainError = "Error:IdentityService:000011";

    public const string AppUserIdentityError = "Error:IdentityService:000100";
    public const string AppUserNotFound = "Error:IdentityService:000101";
    public const string AppUserUsernameDuplicate = "Error:IdentityService:000102";
    public const string AppUserEmailDuplicate = "Error:IdentityService:000103";

    public const string AppRoleIdentityError = "Error:IdentityService:000200";
    public const string AppRoleNotFound = "Error:IdentityService:000201";
    public const string AppRoleNameDuplicate = "Error:IdentityService:000202";
}