using HsnSoft.Base.Domain.Entities;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Entities;

public sealed class AppRolePermission : Entity<Guid>
{
    public Guid AppRoleId { get; set; }

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; }

    private AppRolePermission()
    {
        // Navigation fields
        Permission = null;
    }

    internal AppRolePermission(Guid appRoleId, Guid permissionId) : this(Guid.CreateVersion7(), appRoleId, permissionId)
    {
    }

    internal AppRolePermission(Guid id, Guid appRoleId, Guid permissionId) : this()
    {
        Id = id;
        AppRoleId = appRoleId;
        PermissionId = permissionId;
    }
}