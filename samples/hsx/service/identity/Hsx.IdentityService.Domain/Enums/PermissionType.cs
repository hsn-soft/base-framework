using System.ComponentModel.DataAnnotations;

namespace Hsx.IdentityService.Domain.Enums;

/// <summary>What a permission is for. The code of a permission starts with the type in small letters (<c>page.</c>, <c>endpoint.</c>, <c>operation.</c>, <c>constraint.</c>), so the two cannot disagree.</summary>
public enum PermissionType
{
    /// <summary>Lets a user into a page of the back office (<c>page.users.view</c>); only the front end asks for it.</summary>
    [Display(Name = "Page", Prompt = "page")]
    Page = 1,

    /// <summary>An action of an endpoint (<c>endpoint.app-users.read</c>), asked by <c>[PermissionAuthorize]</c>. A page reads with it (the menu calls that required) or offers a button for it (an action).</summary>
    [Display(Name = "Endpoint", Prompt = "endpoint")]
    Endpoint = 2,

    /// <summary>Something that happens inside an endpoint (<c>operation.subscriptions.settings.view</c>: a field is open or masked), asked by the code that does it (<c>IPermissionChecker</c>).</summary>
    [Display(Name = "Operation", Prompt = "operation")]
    Operation = 3,

    /// <summary>A limit on a value (<c>constraint.audit-logs.max-days</c>): a grant of it carries the value, and a caller with none is refused (framework ADR-0083).</summary>
    [Display(Name = "Constraint", Prompt = "constraint")]
    Constraint = 4,
}
