using Hhs.EventManagerService.Domain.Enums;
using Hhs.EventManagerService.Domain.EventDomain.Consts;
using Hhs.EventManagerService.Domain.EventDomain.Entities;
using Hhs.EventManagerService.Domain.EventDomain.Exceptions;
using Hhs.EventManagerService.Domain.EventDomain.Repositories;
using Hhs.EventManagerService.MongoDb.Context;
using HsnSoft.Base.Domain.Repositories;

namespace Hhs.EventManagerService.MongoDb.Repositories;

public sealed class MongoFailedIntegrationEventRepository : MongoGenericRepository<FailedIntegrationEvent, Guid>, IFailedIntegrationEventRepository
{
    public MongoFailedIntegrationEventRepository(IServiceProvider provider, EventManagerServiceDbContext dbContext) : base(provider, dbContext)
    {
    }

    public async Task<FailedIntegrationEvent> CreateAsync(
        DateTime envelopeTime,
        string failedReason,
        FailedIntegrationEventStates operationStatus,
        string operationStatusDescription = null,
        string correlationId = null,
        string producer = null,
        string channel = null,
        string userId = null,
        string userRoleUniqueName = null,
        Guid? failedMessageEnvelopeId = null,
        DateTime? failedMessageEnvelopeTime = null,
        object failedMessageObject = null,
        string failedMessageTypeName = null,
        ushort hopLevel = 1,
        ushort reQueuedCount = 0)
        => await CreateAsync(id: Guid.CreateVersion7(),
            envelopeTime: envelopeTime,
            failedReason: failedReason,
            operationStatus: operationStatus,
            operationStatusDescription: operationStatusDescription,
            correlationId: correlationId,
            producer: producer,
            channel: channel,
            userId: userId,
            userRoleUniqueName: userRoleUniqueName,
            failedMessageEnvelopeId: failedMessageEnvelopeId,
            failedMessageEnvelopeTime: failedMessageEnvelopeTime,
            failedMessageObject: failedMessageObject,
            failedMessageTypeName: failedMessageTypeName,
            hopLevel: hopLevel,
            reQueuedCount: reQueuedCount
        );

    public async Task<FailedIntegrationEvent> CreateAsync(
        Guid id,
        DateTime envelopeTime,
        string failedReason,
        FailedIntegrationEventStates operationStatus,
        string operationStatusDescription = null,
        string correlationId = null,
        string producer = null,
        string channel = null,
        string userId = null,
        string userRoleUniqueName = null,
        Guid? failedMessageEnvelopeId = null,
        DateTime? failedMessageEnvelopeTime = null,
        object failedMessageObject = null,
        string failedMessageTypeName = null,
        ushort hopLevel = 1,
        ushort reQueuedCount = 0)
    {
        if (id == Guid.Empty) id = Guid.CreateVersion7();

        var draft = new FailedIntegrationEvent(
            id: id,
            envelopeTime: envelopeTime,
            failedReason: failedReason,
            operationStatus: operationStatus,
            operationStatusDescription: operationStatusDescription,
            correlationId: correlationId,
            producer: producer,
            channel: channel,
            userId: userId,
            userRoleUniqueName: userRoleUniqueName,
            failedMessageEnvelopeId: failedMessageEnvelopeId,
            failedMessageEnvelopeTime: failedMessageEnvelopeTime,
            failedMessageObject: failedMessageObject,
            failedMessageTypeName: failedMessageTypeName,
            hopLevel: hopLevel,
            reQueuedCount: reQueuedCount
        );

        //Domain Rules
        // Rule01
        // Rule02
        _ = await InsertAsync(draft);
        return draft;
    }

    public async Task<FailedIntegrationEvent> ReQueuedResultAsync(Guid id, bool isReQueuedSuccess, string errorMessage)
    {
        var oldEntity = await GetByIdOrDefaultAsync(id);
        if (oldEntity == null)
        {
            throw new FailedIntegrationEventNotFoundException(id.ToString());
        }

        if (oldEntity.OperationStatus == FailedIntegrationEventStates.OperationSuccess)
        {
            throw new FailedIntegrationEventStateException(id.ToString());
        }

        if (isReQueuedSuccess)
        {
            oldEntity.OperationStatus = FailedIntegrationEventStates.OperationSuccess;
            oldEntity.OperationStatusDescription = FailedIntegrationEventOperationFacilities.ERROR_HANDLING_SUCCESS;
        }
        else
        {
            oldEntity.OperationStatus = FailedIntegrationEventStates.OperationFail;
            oldEntity.OperationStatusDescription = FailedIntegrationEventOperationFacilities.ERROR_HANDLING_FAILED + " " + errorMessage;
        }

        //Domain Rules
        // Rule01
        // Rule02
        _ = await UpdateAsync(oldEntity);
        return oldEntity;
    }
}