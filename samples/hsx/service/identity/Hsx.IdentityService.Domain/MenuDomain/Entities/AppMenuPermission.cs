using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using Hsx.IdentityService.Domain.Enums;
using Hsx.IdentityService.Domain.PermissionDomain.Entities;

namespace Hsx.IdentityService.Domain.MenuDomain.Entities;

/// <summary>A permission that a menu item has, and what it is to it (<see cref="MenuPermissionRelationType"/>): the visibility that opens the page, what it reads with, its buttons. The map is set again from the code at every start.</summary>
public sealed class AppMenuPermission : Entity<Guid>
{
    // for EF Core
    private AppMenuPermission()
    {
    }

    public Guid MenuId { get; private set; }

    public Guid PermissionId { get; private set; }

    public MenuPermissionRelationType RelationType { get; private set; }

    public int SortOrder { get; private set; }

    // navigations: a collection is empty when it is not loaded, never null; a single one is null when it is not loaded
    public Permission? Permission { get; }

    public static AppMenuPermission Create(Guid menuId, Guid permissionId, MenuPermissionRelationType relationType, int sortOrder, Guid? id = null)
        => new() { Id = id is { } given ? Guard.NotDefault<Guid>(given, nameof(Id)) : Guid.CreateVersion7(), MenuId = menuId, PermissionId = permissionId, RelationType = relationType, SortOrder = sortOrder };
}
