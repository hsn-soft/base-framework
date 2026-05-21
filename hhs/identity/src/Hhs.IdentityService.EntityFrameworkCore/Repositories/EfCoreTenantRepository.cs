using Hhs.IdentityService.Domain.Localization;
using Hhs.IdentityService.Domain.TenantDomain.Entities;
using Hhs.IdentityService.Domain.TenantDomain.Repositories;
using Hhs.IdentityService.EntityFrameworkCore.Context;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.IdentityService.EntityFrameworkCore.Repositories;

public class EfCoreTenantRepository : EfCoreGenericRepository<Tenant, Guid>, ITenantRepository
{
    [NotNull] protected IStringLocalizer L { get; }

    public EfCoreTenantRepository(
        IServiceProvider provider,
        IStringLocalizerFactory stringLocalizerFactory,
        IdentityServiceDbContext dbContext
    ) : base(provider, dbContext)
    {
        L = stringLocalizerFactory.CreateMultiple([typeof(IdentityServiceResource), typeof(ValidationResource), typeof(SharedResource)]);
    }
}