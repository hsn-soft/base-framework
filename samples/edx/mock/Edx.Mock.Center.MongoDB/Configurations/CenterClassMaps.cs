using Edx.Mock.Center.Domain.Envelopes;
using Edx.Mock.Center.Domain.Participants;
using HsnSoft.Base.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Edx.Mock.Center.MongoDB.Configurations;

/// <summary>How the participants and the envelopes are stored: the domain has no attributes of the driver, the mapping is here. The enums are their names.</summary>
public static class CenterClassMaps
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(EnvelopeQueueItem)))
        {
            return;
        }

        MongoEntityMaps.RegisterBases<Guid>();
        BsonClassMap.RegisterClassMap<ParticipantAlias>(map =>
        {
            map.MapProperty(alias => alias.Name);
            map.MapProperty(alias => alias.Type).SetSerializer(new EnumSerializer<AliasType>(BsonType.String));
            map.MapProperty(alias => alias.Endpoint);
        });
        BsonClassMap.RegisterClassMap<Participant>(map =>
        {
            map.MapProperty(participant => participant.RegisterNumber);
            map.MapProperty(participant => participant.Title);
            map.MapProperty(participant => participant.Aliases);
        });
        BsonClassMap.RegisterClassMap<EnvelopeQueueItem>(map =>
        {
            map.MapProperty(item => item.EnvelopeId);
            map.MapProperty(item => item.Origin).SetSerializer(new EnumSerializer<EnvelopeOrigin>(BsonType.String));
            map.MapProperty(item => item.Stage).SetSerializer(new EnumSerializer<EnvelopeStage>(BsonType.String));
            map.MapProperty(item => item.ObjectName);
            map.MapProperty(item => item.ContentLength);
            map.MapProperty(item => item.ReceivedAt);
            map.MapProperty(item => item.StatusCode);
            map.MapProperty(item => item.StatusDescription);
            map.MapProperty(item => item.DocumentType);
            map.MapProperty(item => item.SenderAlias);
            map.MapProperty(item => item.SenderRegisterNumber);
            map.MapProperty(item => item.SenderTitle);
            map.MapProperty(item => item.ReceiverAlias);
            map.MapProperty(item => item.ReceiverRegisterNumber);
            map.MapProperty(item => item.ReceiverTitle);
            map.MapProperty(item => item.ElementType);
            map.MapProperty(item => item.EnvelopeCreatedAt);
            map.MapProperty(item => item.SenderParticipantId);
            map.MapProperty(item => item.ReceiverParticipantId);
            map.MapProperty(item => item.ReferenceEnvelopeId);
            map.MapProperty(item => item.DocumentKeys);
            map.MapProperty(item => item.DocumentCount);
            map.MapProperty(item => item.SendCount);
            map.MapProperty(item => item.LastSendAt);
            map.MapProperty(item => item.NextSendAt);
            map.MapProperty(item => item.LastError);
        });
    }
}
