using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using Microsoft.EntityFrameworkCore.Storage;

namespace HsnSoft.Base.EventBus.EventLog.Services;

public interface IIntegrationEventLogService
{
    Task<IEnumerable<IntegrationEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId);
    Task SaveEventAsync(MessageEnvelope<IIntegrationEventMessage> @event, IDbContextTransaction transaction);
    Task MarkEventAsPublishedAsync(Guid eventId);
    Task MarkEventAsInProgressAsync(Guid eventId);
    Task MarkEventAsFailedAsync(Guid eventId);
}