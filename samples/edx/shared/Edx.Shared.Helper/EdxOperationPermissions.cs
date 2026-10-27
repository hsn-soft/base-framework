namespace Edx.Shared.Helper;

/// <summary>
/// The permissions of what happens inside an endpoint, which the endpoint's own permission does not say: <c>operation.{service}.{subject}.{what}</c> (who may see the email of a user, where a file is kept). The code that does it asks the
/// <c>IPermissionChecker</c>.
/// </summary>
public static class EdxOperationPermissions
{
    public static class Identity
    {
        public const string UserEmailView = "operation.identity.user.email.view";
    }

    public static class Intake
    {
        /// <summary>See where a file is kept (the container, the object name and the folder), not only that it was received.</summary>
        public const string FileStorageLocationView = "operation.intake.file.storage-location.view";
    }
}
