using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.VideoGeneratorService.Domain.MediaDomain.Repositories;

public interface IVideoGeneratorInboxMessageRepository : IMongoGenericRepository<EventInboxMessage, Guid>
{
    Task<List<EventInboxMessage>> GetUnprocessedAsync(CancellationToken cancellationToken = default);
    Task<List<EventInboxMessage>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
}
