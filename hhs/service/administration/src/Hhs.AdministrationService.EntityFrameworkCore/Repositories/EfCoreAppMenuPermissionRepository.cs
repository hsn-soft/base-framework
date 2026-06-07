using Hhs.AdministrationService.Domain.MenuDomain.Entities;
using Hhs.AdministrationService.Domain.MenuDomain.Repositories;
using Hhs.AdministrationService.EntityFrameworkCore.Context;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.AdministrationService.EntityFrameworkCore.Repositories;

public class EfCoreAppMenuPermissionRepository : EfCoreGenericRepository<AppMenuPermission, Guid>, IAppMenuPermissionRepository
{
    [NotNull] private IStringLocalizer L { get; }

    public EfCoreAppMenuPermissionRepository(
        IServiceProvider provider,
        IStringLocalizerFactory stringLocalizerFactory,
        AdministrationServiceDbContext dbContext) : base(provider, dbContext)
    {
        L = stringLocalizerFactory.CreateMultiple([typeof(AdministrationServiceDbContext), typeof(ValidationResource), typeof(SharedResource)]);
    }
}