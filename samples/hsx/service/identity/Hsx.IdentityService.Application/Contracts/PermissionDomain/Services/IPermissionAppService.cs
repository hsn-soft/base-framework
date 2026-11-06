using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos.Filters;

namespace Hsx.IdentityService.Application.Contracts.PermissionDomain.Services;

/// <summary>The catalogue of permissions.</summary>
public interface IPermissionAppService
{
    Task<ListDataResultDto<PermissionDto>> GetFilterListAsync(GetPermissionsFilterDto filterInput, CancellationToken cancellationToken = default);
}
