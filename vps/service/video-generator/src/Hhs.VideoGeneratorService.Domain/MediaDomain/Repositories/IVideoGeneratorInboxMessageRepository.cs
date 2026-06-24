using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.VideoGeneratorService.Domain.MediaDomain.Repositories;

public interface IVideoGeneratorInboxMessageRepository : IMongoGenericRepository<VideoGeneratorInboxMessage, Guid>
{
    Task<List<VideoGeneratorInboxMessage>> GetUnprocessedAsync(CancellationToken cancellationToken = default);
    Task<List<VideoGeneratorInboxMessage>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
}
