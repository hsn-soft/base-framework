using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.AuthDomain.Repositories;
using Hhs.IdentityService.Domain.Localization;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.EntityFrameworkCore.Repositories;

public class EfCoreAppRoleRepository : EfCoreGenericRepository<AppRole, Guid>, IAppRoleRepository
{
    [NotNull] protected IStringLocalizer L { get; }

    public EfCoreAppRoleRepository(
        IServiceProvider provider,
        IStringLocalizerFactory stringLocalizerFactory,
        IdentityServiceDbContext dbContext
    ) : base(provider, dbContext)
    {
        L = stringLocalizerFactory.CreateMultiple([typeof(IdentityServiceResource), typeof(ValidationResource), typeof(SharedResource)]);
    }

    public Task<AppRole> CreateAsync(Guid tenantId, string tenantDomain, string name, bool isDefault, bool isStatic, bool isPublic) => throw new NotImplementedException();

    public Task<AppRole> CreateAsync(Guid id, Guid tenantId, string tenantDomain, string name, bool isDefault, bool isStatic, bool isPublic) => throw new NotImplementedException();

    public Task<AppRole> UpdateAsync(Guid id, string name, bool isDefault, bool isStatic, bool isPublic) => throw new NotImplementedException();

    public Task DeleteAsync(Guid id) => throw new NotImplementedException();
}