using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.VideoGeneratorService.Domain.MediaDomain.Repositories;

public interface IVideoRequestRepository : IMongoGenericRepository<VideoRequest, Guid>
{
    [ItemCanBeNull]
    Task<VideoRequest> GetByScopeKeyAndRefContentAsync(string scopeKey, Guid refContentId, CancellationToken cancellationToken = default);
    Task<List<VideoRequest>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<List<VideoRequest>> GetByScopeKeyAsync(string scopeKey, CancellationToken cancellationToken = default);
}
