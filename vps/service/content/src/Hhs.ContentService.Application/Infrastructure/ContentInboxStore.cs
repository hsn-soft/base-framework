// using System.Text.Json;
// using Hhs.ContentService.Domain.ContentDomain.Entities;
// using Hhs.ContentService.EntityFrameworkCore.Context;
// using Hhs.Shared.Helper;
// using Microsoft.EntityFrameworkCore;
//
// namespace Hhs.ContentService.Application.Infrastructure;
//
// public sealed class ContentInboxStore(ContentServiceDbContext context)
// {
//     public async Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken)
//     {
//         return await context.InboxMessages
//             .AnyAsync(x => x.EventId == eventId && x.Status == InboxStatuses.Completed,
//                 cancellationToken: cancellationToken);
//     }
//
//     public async Task<bool> StartAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
//         where TEvent : IntegrationEvent
//     {
//         if (await IsProcessedAsync(@event.EventId, cancellationToken))
//             return false;
//
//         var existing = await context.InboxMessages
//             .FirstOrDefaultAsync(x => x.EventId == @event.EventId, cancellationToken);
//
//         if (existing is not null)
//         {
//             if (existing.Status != InboxStatuses.Failed)
//                 return false;
//
//             await context.InboxMessages
//                 .Where(x => x.EventId == @event.EventId && x.Status == InboxStatuses.Failed)
//                 .ExecuteUpdateAsync(
//                     s => s
//                         .SetProperty(a => a.Status, InboxStatuses.Started)
//                         .SetProperty(a => a.ErrorMessage, (string?)null)
//                         .SetProperty(a => a.CreatedAtUtc, DateTime.UtcNow),
//                     cancellationToken: cancellationToken);
//
//             return true;
//         }
//
//         context.InboxMessages.Add(new ContentInboxMessage
//         {
//             EventId = @event.EventId,
//             EventName = @event.EventName,
//             Payload = JsonSerializer.Serialize(@event, @event.GetType()),
//             Status = InboxStatuses.Started,
//             CreatedAtUtc = DateTime.UtcNow
//         });
//
//         try
//         {
//             await context.SaveChangesAsync(cancellationToken);
//             return true;
//         }
//         catch (DbUpdateException)
//         {
//             return false;
//         }
//     }
//
//     public async Task CompleteAsync(Guid eventId, CancellationToken cancellationToken)
//     {
//         await context.InboxMessages
//             .Where(x => x.EventId == eventId)
//             .ExecuteUpdateAsync(
//                 s => s
//                     .SetProperty(a => a.Status, InboxStatuses.Completed)
//                     .SetProperty(a => a.ProcessedAtUtc, DateTime.UtcNow)
//                     .SetProperty(a => a.ErrorMessage, (string?)null),
//                 cancellationToken: cancellationToken);
//     }
//
//     public async Task FailAsync(Guid eventId, Exception ex, CancellationToken cancellationToken)
//     {
//         await context.InboxMessages
//             .Where(x => x.EventId == eventId)
//             .ExecuteUpdateAsync(
//                 s => s
//                     .SetProperty(a => a.Status, InboxStatuses.Failed)
//                     .SetProperty(a => a.ErrorMessage, ex.Message),
//                 cancellationToken: cancellationToken);
//     }
// }