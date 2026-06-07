using Hhs.AdministrationService.Domain.old;
using HsnSoft.Base.Domain.Entities;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Entities;

public sealed class LeafMenuPermissionMap : Entity<Guid>
{
    public string ClientId { get; set; }

    public string LeafMenuUniqueName { get; set; }

    public PermissionTypesold PermissionType { get; set; }

    public string PermissionUniqueName { get; set; }

    public byte OrderNo { get; set; }

    public LeafMenuPermissionMap(string clientId, string leafMenuUniqueName, PermissionTypesold permissionType, string permissionUniqueName, byte orderNo)
    {
        Id = Guid.CreateVersion7();
        ClientId = clientId;
        LeafMenuUniqueName = leafMenuUniqueName;
        PermissionType = permissionType;
        PermissionUniqueName = permissionUniqueName;
        OrderNo = orderNo;
    }
}