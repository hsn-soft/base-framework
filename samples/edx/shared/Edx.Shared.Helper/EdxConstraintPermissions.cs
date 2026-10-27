namespace Edx.Shared.Helper;

/// <summary>
/// The permissions that limit a value: <c>constraint.{service}.{subject}.{limit}</c>. A grant carries the value (a number of bytes, a count) the role or the user is held to; whoever was given none is held to the setting of the service.
/// The code that is limited asks the <c>IPermissionChecker</c> for the value.
/// </summary>
public static class EdxConstraintPermissions
{
    public static class Intake
    {
        /// <summary>The most bytes one upload of the caller may weigh; never more than <c>Intake:MaxUploadBytes</c>.</summary>
        public const string UploadMaxBytes = "constraint.intake.upload.max-bytes";
    }
}
