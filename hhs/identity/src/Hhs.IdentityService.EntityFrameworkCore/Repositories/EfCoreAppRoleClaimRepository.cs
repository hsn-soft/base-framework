using Hhs.IdentityService.Domain.AuthDomain.Entities;
using Hhs.IdentityService.Domain.AuthDomain.Repositories;
using Hhs.IdentityService.Domain.Localization;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.Shared.Localization;
using HsnSoft.Base.Data;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.EntityFrameworkCore.Repositories;

public class EfCoreAppRoleClaimRepository : EfCoreGenericRepository<AppRoleClaim, Guid>, IAppRoleClaimRepository
{
    [NotNull] private IStringLocalizer L { get; }

    private readonly IDataFilter _dataFilter;
    private readonly ICurrentTenant _currentTenant;

    public EfCoreAppRoleClaimRepository(
        IServiceProvider provider,
        IStringLocalizerFactory stringLocalizerFactory,
        IdentityServiceDbContext dbContext, IDataFilter dataFilter, ICurrentTenant currentTenant) : base(provider, dbContext)
    {
        L = stringLocalizerFactory.CreateMultiple([typeof(IdentityServiceResource), typeof(ValidationResource), typeof(SharedResource)]);
        _dataFilter = dataFilter;
        _currentTenant = currentTenant;
    }
}