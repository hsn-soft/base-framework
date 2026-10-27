using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Submits;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Exceptions;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using HsnSoft.Base.Application;
using Microsoft.Extensions.Logging;

namespace Edx.IdentityService.Application.Services;

public sealed class UserPermissionAppService(IUserRepository users, IPermissionGrantRepository grants, PermissionCacheSynchronizer synchronizer, IServiceProvider services)
    : ApplicationService(services), IUserPermissionAppService
{
    public async Task<List<GrantedPermissionDto>> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _ = await FindAsync(userId, cancellationToken).ConfigureAwait(false);
        return GrantMapping.ToDtos(await grants.GetUserGrantsAsync(userId, cancellationToken).ConfigureAwait(false));
    }

    public async Task SetAsync(Guid userId, SetGrantsDto input, CancellationToken cancellationToken = default)
    {
        User user = await FindAsync(userId, cancellationToken).ConfigureAwait(false);
        List<(Permission Permission, string? Value)> resolved = await GrantMapping.ResolveAsync(grants, input, cancellationToken).ConfigureAwait(false);
        await grants.ReplaceUserGrantsAsync(user, resolved, cancellationToken).ConfigureAwait(false);
        await synchronizer.SyncUserAsync(user, cancellationToken).ConfigureAwait(false);
        Logger.LogInformation("The grants of the user {GrantedUserId} were set: {GrantCount} permissions", user.Id, resolved.Count);
    }

    private async Task<User> FindAsync(Guid userId, CancellationToken cancellationToken)
        => await users.FindAsync(userId, cancellationToken).ConfigureAwait(false) ?? throw UserNotFound.For(userId);
}
