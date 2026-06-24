using Hhs.ContentService.Domain.ContentDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.ContentService.Domain.ContentDomain.Repositories;

public interface IAnalysisContentRepository : IGenericRepository<AnalysisContent, Guid>
{
    Task<AnalysisContent?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AnalysisContent?> GetByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default);
    Task<List<AnalysisContent>> GetAllByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default);
}
