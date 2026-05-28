using Hhs.AdministrationService.Domain.Enums;
using HsnSoft.Base.Domain.Entities;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Entities;

public sealed class LeafMenuPermissionMap : Entity<Guid>
{
    public string ClientId { get; set; }

    public string LeafMenuUniqueName { get; set; }

    public PermissionTypes PermissionType { get; set; }

    public string PermissionUniqueName { get; set; }

    public byte OrderNo { get; set; }

    public LeafMenuPermissionMap(string clientId, string leafMenuUniqueName, PermissionTypes permissionType, string permissionUniqueName, byte orderNo)
    {
        Id = Guid.CreateVersion7();
        ClientId = clientId;
        LeafMenuUniqueName = leafMenuUniqueName;
        PermissionType = permissionType;
        PermissionUniqueName = permissionUniqueName;
        OrderNo = orderNo;
    }
}