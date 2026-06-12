using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.Enums;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Repositories;

public interface ICustomerVideoGenerationHistory : IReadOnlyGenericRepository<CustomerVideoGenerationHistory, Guid>
{
    Task<long> GetCustomerVideoHistoryCountAsync(Guid customerId, DateTime videoGenerationDate, VideoGenerationTypes? videoGenerationType = null);

    Task<CustomerVideoGenerationHistory> CreateAsync(Guid tenantId, Guid customerId, DateTime videoGenerationDate, VideoGenerationTypes videoGenerationType, [NotNull] string contentReferenceIds);
}