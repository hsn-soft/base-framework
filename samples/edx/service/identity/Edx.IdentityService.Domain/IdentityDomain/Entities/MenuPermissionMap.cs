namespace Edx.IdentityService.Domain.IdentityDomain.Entities;

/// <summary>A permission that a menu item has, and what it is to it (<see cref="MenuPermissionRelationType"/>): the visibility that opens the page, what it reads with, its buttons, what it hides field by field, its limits. The map is how the client knows what to ask and what to show.</summary>
public sealed class MenuPermissionMap
{
    // for EF Core
    private MenuPermissionMap()
    {
    }

    public Guid Id { get; private set; }

    public Guid MenuId { get; private set; }

    public Guid PermissionId { get; private set; }

    public MenuPermissionRelationType RelationType { get; private set; }

    public int SortOrder { get; private set; }

    public static MenuPermissionMap Create(Guid menuId, Guid permissionId, MenuPermissionRelationType relationType, int sortOrder)
        => new() { Id = Guid.CreateVersion7(), MenuId = menuId, PermissionId = permissionId, RelationType = relationType, SortOrder = sortOrder };
}
