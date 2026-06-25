using HsnSoft.Base.MongoDB.Helpers;
using Hhs.EventManagerService.Domain.EventDomain.Consts;
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

            // Correlation & Tracing
            map.MapMember(x => x.CorrelationId)
                .SetMaxLength(FailedIntegrationEventConsts.CorrelationIdMaxLength);

            // Error Handling
            map.MapMember(x => x.FailedReason)
                .SetIsRequired(true)
                .SetMaxLength(FailedIntegrationEventConsts.FailedReasonMaxLength);

            // OperationStatusDescription
            map.MapMember(x => x.OperationStatusDescription)
                .SetMaxLength(FailedIntegrationEventConsts.OperationStatusDescriptionMaxLength);

            // Provider Information
            map.MapMember(x => x.Producer)
                .SetMaxLength(FailedIntegrationEventConsts.ProducerMaxLength);
            map.MapMember(x => x.Channel)
                .SetMaxLength(FailedIntegrationEventConsts.ChannelMaxLength);

            // User Information
            map.MapMember(x => x.UserId)
                .SetMaxLength(FailedIntegrationEventConsts.UserIdMaxLength);
            map.MapMember(x => x.UserRoleUniqueName)
                .SetMaxLength(FailedIntegrationEventConsts.UserRoleUniqueNameMaxLength);

            // Message Information
            map.MapMember(x => x.FailedMessageTypeName)
                .SetMaxLength(FailedIntegrationEventConsts.FailedMessageTypeNameMaxLength);
        });
    }
}