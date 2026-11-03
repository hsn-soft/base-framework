using Hsx.IdentityService.Application.Contracts.PermissionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.PermissionDomain.Services;
using Hsx.IdentityService.Domain.PermissionDomain.Repositories;

namespace Hsx.IdentityService.Application.Services;

public sealed class SessionPermissionAppService(IPermissionGrantRepository grants, IServiceProvider services) : ApplicationServiceBase(services), ISessionPermissionAppService
{
    public async Task<List<GrantedPermissionDto>> GetMineAsync(CancellationToken cancellationToken = default)
        => CurrentUser.Id is { } id
            ? GrantMapping.ToDtos(await grants.GetEffectiveGrantsAsync(id, cancellationToken).ConfigureAwait(false))
            : [];
}
