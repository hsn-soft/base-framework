using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.VideoGeneratorService.Domain.MediaDomain.Repositories;

public interface IAudioRequestRepository : IMongoGenericRepository<AudioRequest, Guid>
{
    Task<List<AudioRequest>> GetByVideoRequestIdAsync(Guid videoRequestId, CancellationToken cancellationToken = default);
    Task<List<AudioRequest>> GetByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default);
    Task<List<AudioRequest>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
}
