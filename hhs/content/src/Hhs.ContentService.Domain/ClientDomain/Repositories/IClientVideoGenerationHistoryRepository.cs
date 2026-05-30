using Hhs.ContentService.Domain.ClientDomain.Entities;
using Hhs.ContentService.Domain.Enums;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ClientDomain.Repositories;

public interface IClientVideoGenerationHistoryRepository : IReadOnlyGenericRepository<ClientVideoGenerationHistory, Guid>
{
    Task<long> GetClientVideoHistoryCountAsync(Guid clientId, DateTime videoGenerationDate, VideoGenerationTypes? videoGenerationType = null);

    Task<ClientVideoGenerationHistory> CreateAsync(Guid clientId, DateTime videoGenerationDate, VideoGenerationTypes videoGenerationType, [NotNull] string contentReferenceIds);
}