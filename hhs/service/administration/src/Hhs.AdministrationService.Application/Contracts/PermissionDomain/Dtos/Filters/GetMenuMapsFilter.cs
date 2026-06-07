using JetBrains.Annotations;

namespace Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Filters;

public sealed class GetMenuMapsFilter
{
    [CanBeNull]
    public string ClientMenuType { get; set; } = null;

    public bool? ShowSystemMenus { get; set; }= null;
}