using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using HsnSoft.Base.Application;

namespace Edx.IdentityService.Application.Services;

public sealed class SessionPermissionAppService(IPermissionGrantRepository grants, IServiceProvider services) : ApplicationService(services), ISessionPermissionAppService
{
    public async Task<List<GrantedPermissionDto>> GetMineAsync(CancellationToken cancellationToken = default)
        => CurrentUser.Id is { } id
            ? GrantMapping.ToDtos(await grants.GetEffectiveGrantsAsync(id, cancellationToken).ConfigureAwait(false))
            : [];
}
