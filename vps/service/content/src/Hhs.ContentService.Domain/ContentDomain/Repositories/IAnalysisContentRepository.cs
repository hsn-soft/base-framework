using Hhs.ContentService.Domain.ContentDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Repositories;

public interface IAnalysisContentRepository : IGenericRepository<AnalysisContent, Guid>
{
    Task SetNormalizedReferenceAsync(Guid id, Guid normalizedRequestId);

    Task SetVideoReferenceAsync(Guid id, Guid videoRequestId);

    Task SetVideoGenerationApprovedAsync(Guid id);

    [ItemCanBeNull]
    Task<AnalysisContent> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    [ItemCanBeNull]
    Task<AnalysisContent> GetByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default);

    Task<List<AnalysisContent>> GetAllByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default);
}