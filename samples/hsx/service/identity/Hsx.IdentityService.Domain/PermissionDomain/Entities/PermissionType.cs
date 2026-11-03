namespace Hsx.IdentityService.Domain.PermissionDomain.Entities;

/// <summary>What a permission is for. The code of a permission starts with the type in small letters (<c>page.</c>, <c>endpoint.</c>, <c>operation.</c>), so the two cannot disagree.</summary>
public enum PermissionType
{
    /// <summary>Lets a user into a page of the back office (<c>page.users.view</c>); only the front end asks for it.</summary>
    Page = 1,

    /// <summary>An action of an endpoint (<c>endpoint.app-users.read</c>), asked by <c>[PermissionAuthorize]</c>. A page reads with it (the menu calls that required) or offers a button for it (an action).</summary>
    Endpoint = 2,

    /// <summary>Something that happens inside an endpoint (<c>operation.subscriptions.settings.view</c>: a field is open or masked), asked by the code that does it. The system has none yet.</summary>
    Operation = 3,
}
