using System.ComponentModel.DataAnnotations;

namespace Hsx.IdentityService.Domain.Enums;

/// <summary>What an item of a menu is.</summary>
public enum MenuNodeType
{
    /// <summary>A label that sets a group of items apart: only at the root, and it takes the user nowhere.</summary>
    [Display(Name = "Caption", Prompt = "caption")]
    Caption = 1,

    /// <summary>An item that opens and closes, with items under it.</summary>
    [Display(Name = "Node", Prompt = "node")]
    Node = 2,

    /// <summary>An item to click, which takes the user to a page: at the root or under a node.</summary>
    [Display(Name = "Leaf", Prompt = "leaf")]
    Leaf = 3,
}
