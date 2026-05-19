using Hhs.AdministrationService.Domain.Enums;
using HsnSoft.Base.Application.Dtos;
using JetBrains.Annotations;

namespace Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;

public sealed class MenuMapDto : EntityDto<Guid>
{
    public string ClientMenuType { get; set; }

    [CanBeNull]
    public string ParentUniqueName { get; set; }

    public string UniqueName { get; set; }

    public MenuMapType MapType { get; set; }

    public string Url { get; set; }

    public string Icon { get; set; }

    public byte OrderNo { get; set; }
    public string Hierarchy { get; set; }
}