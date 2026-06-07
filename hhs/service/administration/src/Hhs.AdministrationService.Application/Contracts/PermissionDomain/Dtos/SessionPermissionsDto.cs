namespace Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;

public sealed class SessionPermissionsDto
{
    public string Client { get; set; }

    public string[] Roles { get; set; }

    public string User { get; set; }

    public List<SessionPermissionItemDto> Permissions { get; set; }

    public List<SessionMenuChannelDto> ChannelMenus { get; set; }
}

public sealed class SessionPermissionItemDto
{
    public string PermissionUniqueName { get; }

    public List<string> ProviderKeys { get; }

    private SessionPermissionItemDto()
    {
        PermissionUniqueName = string.Empty;
        ProviderKeys = new List<string>();
    }

    public SessionPermissionItemDto(string permissionUniqueName, string providerKey) : this()
    {
        PermissionUniqueName = permissionUniqueName;
        if (!string.IsNullOrWhiteSpace(providerKey)) ProviderKeys.Add(providerKey);
    }

    public SessionPermissionItemDto(string permissionUniqueName, List<string> providerKeys) : this()
    {
        PermissionUniqueName = permissionUniqueName;
        if (providerKeys is { Count: > 0 }) ProviderKeys = providerKeys;
    }
}

public sealed class SessionMenuChannelDto
{
    public string Channel { get; set; }

    public List<SessionMenuNodeDto> MenuNodes { get; set; }
}

public sealed class SessionMenuNodeDto
{
    public string UniqueName { get; set; }

    public string Url { get; set; }

    public string Icon { get; set; }

    public byte OrderNumber { get; set; }
    public string OrderHierarchy { get; set; }

    public bool Leaf { get; set; }
    public bool Caption { get; set; }

    public List<SessionMenuNodeDto> Childs { get; set; }
}