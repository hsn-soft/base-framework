using Hhs.IdentityService.Domain.AppUserDomain.Entities;
using Hhs.IdentityService.Domain.AppUserDomain.Repositories;
using Hhs.IdentityService.Domain.Localization;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.EntityFrameworkCore.Repositories;

public class EfCoreAppUserRoleRepository : EfCoreGenericRepository<AppUserRole, Guid>, IAppUserRoleRepository
{
    [NotNull] private IStringLocalizer L { get; }

    public EfCoreAppUserRoleRepository(
        IServiceProvider provider,
        IStringLocalizerFactory stringLocalizerFactory,
        IdentityServiceDbContext dbContext
    ) : base(provider, dbContext)
    {
        L = stringLocalizerFactory.CreateMultiple([typeof(IdentityServiceResource), typeof(ValidationResource), typeof(SharedResource)]);
    }

    public async Task<AppUserRole> CreateAsync(Guid tenantId, Guid userId, Guid roleId)
    {
        var draft = new AppUserRole(tenantId, userId, roleId);

        _ = await InsertAsync(draft);

        return draft;
    }

    public async Task<int> CreateManyAsync(Guid tenantId, Guid userId, List<Guid> roleIds)
    {
        List<AppUserRole> draftList = [];

        draftList.AddRange(roleIds.Select(roleId => new AppUserRole(tenantId, userId, roleId)));

        return await InsertManyAsync(draftList);
    }

    public async Task RemoveManyWithUserId(Guid userId) => await DeleteManyAsync(x => x.UserId == userId);
}