using Hhs.ContentService.Domain.Localization;
using Hhs.ContentService.Domain.SettingDomain.Entities;
using Hhs.ContentService.Domain.SettingDomain.Repositories;
using Hhs.ContentService.EntityFrameworkCore.Context;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Validation.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.Localization;

namespace Hhs.ContentService.EntityFrameworkCore.Repositories;

public sealed class EfCoreCustomerVpSettingRepository(
    IServiceProvider provider,
    IStringLocalizerFactory stringLocalizerFactory,
    ContentServiceDbContext dbContext
) : EfCoreGenericRepository<CustomerVpSetting, Guid>(provider, dbContext),
    ICustomerVpSettingRepository
{
    [NotNull]
    protected IStringLocalizer L { get; } = stringLocalizerFactory.CreateMultiple
    (
        [
            typeof(ContentServiceResource),
            typeof(ValidationResource),
            typeof(SharedResource)
        ]
    );
}