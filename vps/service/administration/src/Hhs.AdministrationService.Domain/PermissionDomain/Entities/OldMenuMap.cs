using Hhs.AdministrationService.Domain.old;
using HsnSoft.Base.Domain.Entities;
using JetBrains.Annotations;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Entities;

public sealed class OldMenuMap : Entity<Guid>
{
    public string ClientId { get; set; }
    public string ClientMenuType { get; set; }

    [CanBeNull]
    public string ParentUniqueName { get; set; }

    public string UniqueName { get; set; }

    public MenuMapType MapType { get; set; }

    public string Url { get; set; }

    public string Icon { get; set; }

    public byte OrderNo { get; set; }
    public string Hierarchy { get; set; }

    public bool OnlyAccessSystemUsers { get; set; }

    public OldMenuMap(string clientId, string clientMenuType, string parentUniqueName, string uniqueName, MenuMapType mapType, string url, string icon,
        byte orderNo, string hierarchy, bool onlyAccessSystemUsers = false)
    {
        Id = Guid.CreateVersion7();
        ClientId = clientId;
        ClientMenuType = clientMenuType;
        ParentUniqueName = parentUniqueName;
        UniqueName = uniqueName;
        MapType = mapType;
        Url = url;
        Icon = icon;
        OrderNo = orderNo;
        Hierarchy = hierarchy;
        OnlyAccessSystemUsers = onlyAccessSystemUsers;
    }
}