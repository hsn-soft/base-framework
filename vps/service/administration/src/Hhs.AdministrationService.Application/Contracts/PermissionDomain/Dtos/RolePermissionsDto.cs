namespace Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;

public sealed class RolePermissionsDto
{
    public string RoleUniqueName { get; set; }

    public List<RoleMenuChannelDto> ChannelMenus { get; set; }
}

public sealed class RoleMenuChannelDto
{
    public string Channel { get; set; }

    public List<RolePermissionNodeDto> MenuNodes { get; set; }
}

public sealed class RolePermissionNodeDto
{
    public bool IsGranted { get; set; }

    public string MenuMapLocalizedName { get; set; }

    public string Icon { get; set; }

    public byte OrderNumber { get; set; }
    public string OrderHierarchy { get; set; }

    public string RequiredPermissionUniqueName { get; set; }
    public bool HasLink { get; set; }

    public List<RolePermissionNodeDto> Childs { get; set; }
}