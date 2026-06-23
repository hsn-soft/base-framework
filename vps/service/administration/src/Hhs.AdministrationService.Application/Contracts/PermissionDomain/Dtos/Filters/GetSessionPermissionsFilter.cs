using JetBrains.Annotations;

namespace Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Filters;

public sealed class GetSessionPermissionsFilter
{
    [CanBeNull]
    public string ClientKey { get; set; } = null;

    [CanBeNull]
    public string[] RoleKeys { get; set; } = null;

    [CanBeNull]
    public string UserKey { get; set; } = null;
}