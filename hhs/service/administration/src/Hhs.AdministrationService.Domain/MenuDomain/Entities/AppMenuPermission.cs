using Hhs.AdministrationService.Domain.Enums;
using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using HsnSoft.Base.Domain.Entities;

namespace Hhs.AdministrationService.Domain.MenuDomain.Entities;

public sealed class AppMenuPermission : Entity<Guid>
{
    public Guid AppMenuId { get; set; }
    public AppMenu AppMenu { get; set; }

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; }

    public MenuPermissionRelationTypes RelationType { get; set; }

    public int SortOrder { get; set; }

    private AppMenuPermission()
    {
        // Navigation fields
        AppMenu = null;
        Permission = null;
    }

    internal AppMenuPermission(Guid appMenuId, Guid permissionId, MenuPermissionRelationTypes relationType, int sortOrder = 0)
        : this(Guid.CreateVersion7(), appMenuId, permissionId, relationType, sortOrder)
    {
    }

    internal AppMenuPermission(Guid id, Guid appMenuId, Guid permissionId, MenuPermissionRelationTypes relationType, int sortOrder = 0) : this()
    {
        Id = id;
        AppMenuId = appMenuId;
        PermissionId = permissionId;
        RelationType = relationType;
        SortOrder = sortOrder;
    }
}