using Hhs.EventManagerService.Domain.EventDomain.Entities;
using MongoDB.Bson.Serialization;

namespace Hhs.EventManagerService.MongoDb.Configurations;

public static class FailedIntegrationEventClassMap
{
    public static void Register()
    {
        BsonClassMap.RegisterClassMap<FailedIntegrationEvent>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);

            // Correlation & Tracing - MaxLength: FailedIntegrationEventConsts.CorrelationIdMaxLength

            // Error Handling
            // MaxLength: FailedIntegrationEventConsts.FailedReasonMaxLength
            map.MapMember(x => x.FailedReason).SetIsRequired(true);

            // OperationStatusDescription - MaxLength: FailedIntegrationEventConsts.OperationStatusDescriptionMaxLength

            // Provider Information
            // MaxLength: FailedIntegrationEventConsts.ProducerMaxLength
            // MaxLength: FailedIntegrationEventConsts.ChannelMaxLength

            // User Information
            // MaxLength: FailedIntegrationEventConsts.UserIdMaxLength
            // MaxLength: FailedIntegrationEventConsts.UserRoleUniqueNameMaxLength

            // Message Information - MaxLength: FailedIntegrationEventConsts.FailedMessageTypeNameMaxLength
        });
    }
}