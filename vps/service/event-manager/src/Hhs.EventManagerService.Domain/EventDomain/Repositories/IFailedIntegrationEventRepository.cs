using Hhs.EventManagerService.Domain.Enums;
using Hhs.EventManagerService.Domain.EventDomain.Entities;
using HsnSoft.Base.Domain.Repositories;
using JetBrains.Annotations;

namespace Hhs.EventManagerService.Domain.EventDomain.Repositories;

public interface IFailedIntegrationEventRepository : IReadOnlyGenericRepository<FailedIntegrationEvent, Guid>
{
    Task<FailedIntegrationEvent> CreateAsync(
        DateTime envelopeTime,
        [NotNull] string failedReason,
        FailedIntegrationEventStates operationStatus,
        [CanBeNull] string operationStatusDescription = null,
        [CanBeNull] string correlationId = null,
        [CanBeNull] string producer = null,
        [CanBeNull] string channel = null,
        [CanBeNull] string userId = null,
        [CanBeNull] string userRoleUniqueName = null,
        Guid? failedMessageEnvelopeId = null,
        DateTime? failedMessageEnvelopeTime = null,
        [CanBeNull] object failedMessageObject = null,
        [CanBeNull] string failedMessageTypeName = null,
        ushort hopLevel = 1,
        ushort reQueuedCount = 0
    );

    Task<FailedIntegrationEvent> CreateAsync(
        Guid id,
        DateTime envelopeTime,
        [NotNull] string failedReason,
        FailedIntegrationEventStates operationStatus,
        [CanBeNull] string operationStatusDescription = null,
        [CanBeNull] string correlationId = null,
        [CanBeNull] string producer = null,
        [CanBeNull] string channel = null,
        [CanBeNull] string userId = null,
        [CanBeNull] string userRoleUniqueName = null,
        Guid? failedMessageEnvelopeId = null,
        DateTime? failedMessageEnvelopeTime = null,
        [CanBeNull] object failedMessageObject = null,
        [CanBeNull] string failedMessageTypeName = null,
        ushort hopLevel = 1,
        ushort reQueuedCount = 0
    );

    Task<FailedIntegrationEvent> ReQueuedResultAsync(Guid id, bool isReQueuedSuccess, [CanBeNull] string errorMessage);

}