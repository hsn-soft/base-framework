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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.EntityFrameworkCore.Repositories;

public class EfCoreAuthRefreshTokenRepository : EfCoreGenericRepository<AuthRefreshToken, Guid>, IAuthRefreshTokenRepository
{
    [NotNull] private IStringLocalizer L { get; }

    private readonly IDataFilter _dataFilter;
    private readonly ICurrentTenant _currentTenant;

    public EfCoreAuthRefreshTokenRepository(
        IServiceProvider provider,
        IStringLocalizerFactory stringLocalizerFactory,
        IdentityServiceDbContext dbContext, IDataFilter dataFilter, ICurrentTenant currentTenant) : base(provider, dbContext)
    {
        L = stringLocalizerFactory.CreateMultiple([typeof(IdentityServiceResource), typeof(ValidationResource), typeof(SharedResource)]);
        _dataFilter = dataFilter;
        _currentTenant = currentTenant;
    }

    public async Task RevokeRefreshTokenAsync(string refreshTokenHash, string replacedByTokenHash) =>
        await GetDbSet().Where(b => b.TokenHash == refreshTokenHash).ExecuteUpdateAsync(s => s
            .SetProperty(a => a.RevokedAt, DateTime.UtcNow)
            .SetProperty(a => a.ReplacedByTokenHash, replacedByTokenHash)
        );
}