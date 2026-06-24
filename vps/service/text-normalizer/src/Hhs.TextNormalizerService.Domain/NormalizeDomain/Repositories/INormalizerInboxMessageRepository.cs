using System.Linq.Expressions;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.TextNormalizerService.Domain.NormalizeDomain.Repositories;

public interface INormalizerInboxMessageRepository : IGenericRepository<NormalizerInboxMessage, Guid>
{
    Task<List<NormalizerInboxMessage>> GetUnprocessedAsync(CancellationToken cancellationToken = default);
    Task<List<NormalizerInboxMessage>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
}
