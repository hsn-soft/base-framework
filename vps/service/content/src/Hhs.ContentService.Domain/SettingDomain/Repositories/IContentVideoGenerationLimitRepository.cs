using Hhs.ContentService.Domain.Enums;
using Hhs.ContentService.Domain.SettingDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.SettingDomain.Repositories;

public interface IContentVideoGenerationLimitRepository : IReadOnlyGenericRepository<ContentVideoGenerationLimit, Guid>
{
    Task<long> GetCustomerVideoHistoryCountAsync([NotNull] string scopeKey,
        DateTime videoGenerationDate, VideoGenerationTypes? videoGenerationType = null);

    Task<ContentVideoGenerationLimit> CreateAsync([NotNull] string scopeKey,
        DateTime videoGenerationDate, VideoGenerationTypes videoGenerationType, [NotNull] string contentReferenceIds);
}