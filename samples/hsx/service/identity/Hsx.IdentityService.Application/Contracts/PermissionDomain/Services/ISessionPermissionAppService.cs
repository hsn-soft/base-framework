using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;

namespace Hsx.IdentityService.Application.Contracts.PermissionDomain.Services;

/// <summary>What the caller was granted, through its roles.</summary>
public interface ISessionPermissionAppService
{
    Task<MyPermissionsDto> GetMineAsync(CancellationToken cancellationToken = default);
}
