namespace Edx.IdentityService.Domain.IdentityDomain.Entities;

/// <summary>The client a menu is for.</summary>
public enum MenuChannelType
{
    Mobile = 1,
    BackOffice = 2,
}

/// <summary>Where a menu is shown.</summary>
public enum MenuDockType
{
    SidebarMenu = 1,
    TopbarMenu = 2,
}

/// <summary>What an item of a menu is.</summary>
public enum MenuNodeType
{
    /// <summary>A label that sets a group of items apart: only at the root, and it takes the user nowhere.</summary>
    Caption = 1,

    /// <summary>An item that opens and closes, with items under it.</summary>
    Node = 2,

    /// <summary>An item to click, which takes the user to a page: at the root or under a node.</summary>
    Leaf = 3,
}

/// <summary>What a permission means to a menu item.</summary>
public enum MenuPermissionRelationType
{
    /// <summary>Needed for the item to be in the menu and for the page to open (a <c>page.</c> permission).</summary>
    Visibility = 1,

    /// <summary>Needed for the page to work: what it reads with (an <c>endpoint.</c> permission).</summary>
    Required = 2,

    /// <summary>An optional endpoint of the page, a button: create, delete (an <c>endpoint.</c> permission).</summary>
    Action = 4,

    /// <summary>What the page shows or hides field by field (an <c>operation.</c> permission).</summary>
    Operation = 5,

    /// <summary>A limit of the page at run time (a <c>constraint.</c> permission).</summary>
    Constraint = 6,
}
