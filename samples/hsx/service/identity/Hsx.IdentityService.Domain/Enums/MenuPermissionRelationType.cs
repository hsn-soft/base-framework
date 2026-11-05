using System.ComponentModel.DataAnnotations;

namespace Hsx.IdentityService.Domain.Enums;

/// <summary>What a permission means to a menu item.</summary>
public enum MenuPermissionRelationType
{
    /// <summary>Needed for the item to be in the menu and for the page to open (a <c>page.</c> permission).</summary>
    [Display(Name = "Visibility", Prompt = "visibility")]
    Visibility = 1,

    /// <summary>Needed for the page to work: what it reads with (an <c>endpoint.</c> permission).</summary>
    [Display(Name = "Required", Prompt = "required")]
    Required = 2,

    /// <summary>An optional endpoint of the page, a button: create, delete (an <c>endpoint.</c> permission).</summary>
    [Display(Name = "Action", Prompt = "action")]
    Action = 4,

    /// <summary>What the page shows or hides field by field (an <c>operation.</c> permission).</summary>
    [Display(Name = "Operation", Prompt = "operation")]
    Operation = 5,

    /// <summary>A limit of the page at run time (a <c>constraint.</c> permission).</summary>
    [Display(Name = "Constraint", Prompt = "constraint")]
    Constraint = 6,
}
