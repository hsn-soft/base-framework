using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.Enums;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Repositories;

public interface IContentVideoGenerationLimitRepository : IReadOnlyGenericRepository<ContentVideoGenerationLimit, Guid>
{
    Task<long> GetCustomerVideoHistoryCountAsync([NotNull] string scopeKey,
        DateTime videoGenerationDate, VideoGenerationTypes? videoGenerationType = null);

    Task<ContentVideoGenerationLimit> CreateAsync([NotNull] string scopeKey,
        DateTime videoGenerationDate, VideoGenerationTypes videoGenerationType, [NotNull] string contentReferenceIds);
}