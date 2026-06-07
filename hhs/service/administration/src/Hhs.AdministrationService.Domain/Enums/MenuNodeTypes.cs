using System.ComponentModel.DataAnnotations;

namespace Hhs.AdministrationService.Domain.Enums;

public enum MenuNodeTypes
{
    [Display(Name = "Unknown", Prompt = "unknown")]
    Unknown = 0,

    [Display(Name = "Caption", Prompt = "caption")]
    Caption = 1,

    [Display(Name = "Node", Prompt = "menu")]
    Node = 2,

    [Display(Name = "Leaf", Prompt = "page")]
    Leaf = 3,
}