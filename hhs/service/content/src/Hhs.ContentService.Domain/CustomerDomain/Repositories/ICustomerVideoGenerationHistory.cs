using Hhs.ContentService.Domain.CustomerDomain.Entities;
using Hhs.ContentService.Domain.Enums;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.CustomerDomain.Repositories;

public interface ICustomerVideoGenerationHistory : IReadOnlyGenericRepository<CustomerVideoGenerationHistory, Guid>
{
    Task<long> GetCustomerVideoHistoryCountAsync(Guid customerId, DateTime videoGenerationDate, VideoGenerationTypes? videoGenerationType = null);

    Task<CustomerVideoGenerationHistory> CreateAsync(Guid tenantId, Guid customerId, DateTime videoGenerationDate, VideoGenerationTypes videoGenerationType, [NotNull] string contentReferenceIds);
}