using HsnSoft.Base.Domain.Entities;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Entities;

public class PermissionDependency : Entity<Guid>
{
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; }

    public Guid DependsOnPermissionId { get; set; }
    public Permission DependsOnPermission { get; set; }

    private PermissionDependency()
    {
        // Navigation fields
        Permission = null;
        DependsOnPermission = null;
    }

    internal PermissionDependency(Guid permissionId, Guid dependsOnPermissionId)
        : this(Guid.CreateVersion7(), permissionId, dependsOnPermissionId)
    {
    }

    internal PermissionDependency(Guid id, Guid permissionId, Guid dependsOnPermissionId) : this()
    {
        Id = id;
        PermissionId = permissionId;
        DependsOnPermissionId = dependsOnPermissionId;
    }
}