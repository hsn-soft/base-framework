namespace Edx.IdentityService.Domain.IdentityDomain.Entities;

/// <summary>What a permission is for. The code of a permission starts with the type, in small letters, so the two cannot disagree.</summary>
public enum PermissionType
{
    /// <summary>An action of an endpoint (<c>endpoint.intake.files.read</c>), asked by <c>[PermissionAuthorize]</c>.</summary>
    Endpoint = 0,

    /// <summary>Something that happens inside an endpoint (<c>operation.identity.user.email.view</c>), asked by the code that does it.</summary>
    Operation = 1,

    /// <summary>A limit on a value (<c>constraint.intake.upload.max-bytes</c>): a grant of it carries the value.</summary>
    Constraint = 2,
}
