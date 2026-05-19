using System.ComponentModel.DataAnnotations;

namespace Hhs.AdministrationService.Domain.Enums;

public enum MenuMapType
{
    // [Display(Name = "NodeMenu")]
    // NodeMenu = 0,
    //
    // [Display(Name = "NodeLeafNew")]
    // NodeLeafItem = 1,
    //
    // [Display(Name = "NodeLeafMenu")]
    // NodeLeafMenu = 2,
    //
    // [Display(Name = "NodeLeafActionPermission")]
    // NodeLeafActionPermission = 3,
    //
    // [Display(Name = "NodeLeafActionMenu")]
    // NodeLeafActionMenu = 4,


    [Display(Name = "Caption")]
    Caption = 0,

    [Display(Name = "Node")]
    Node = 1,

    [Display(Name = "Leaf")]
    Leaf = 2,
}

public enum PermissionTypes
{
    [Display(Name = "LeafMenuPermission")]
    LeafMenuPermission = 0,

    [Display(Name = "ActionPermission")]
    ActionPermission = 1,

    [Display(Name = "OperationPermission")]
    OperationPermission = 2,
}