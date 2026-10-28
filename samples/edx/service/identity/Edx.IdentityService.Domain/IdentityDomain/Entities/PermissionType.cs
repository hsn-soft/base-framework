namespace Edx.IdentityService.Domain.IdentityDomain.Entities;

/// <summary>What a permission is for. The code of a permission starts with the type in small letters (<c>page.</c>, <c>endpoint.</c>, <c>operation.</c>, <c>constraint.</c>), so the two cannot disagree.</summary>
public enum PermissionType
{
    /// <summary>Lets a user into a page of a back office (<c>page.invoice.view</c>); only the front end asks for it.</summary>
    Page = 1,

    /// <summary>An action of an endpoint (<c>endpoint.invoice.read</c>), asked by <c>[PermissionAuthorize]</c>. A page reads with it (the menu calls that required) or offers a button for it (an action).</summary>
    Endpoint = 2,

    /// <summary>Something that happens inside an endpoint (<c>operation.invoice.price.view</c>), asked by the code that does it.</summary>
    Operation = 3,

    /// <summary>A limit on a value (<c>constraint.invoice.max-view-amount</c>): a grant of it carries the value, and a caller with none is refused.</summary>
    Constraint = 4,
}
