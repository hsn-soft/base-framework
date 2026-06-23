using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using Hhs.AdministrationService.Domain.PermissionDomain.Repositories;
using Hhs.AdministrationService.EntityFrameworkCore.Context;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.AdministrationService.EntityFrameworkCore.Repositories;

public class EfCorePermissionDependencyRepository : EfCoreGenericRepository<PermissionDependency, Guid>, IPermissionDependencyRepository
{
    [NotNull] private IStringLocalizer L { get; }

    public EfCorePermissionDependencyRepository(
        IServiceProvider provider,
        IStringLocalizerFactory stringLocalizerFactory,
        AdministrationServiceDbContext dbContext) : base(provider, dbContext)
    {
        L = stringLocalizerFactory.CreateMultiple([typeof(AdministrationServiceDbContext), typeof(ValidationResource), typeof(SharedResource)]);
    }
}