using Edx.Integration.GibInvoice.Domain.DirectoryDomain.Entities;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;
using HsnSoft.Base.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Edx.Integration.GibInvoice.MongoDB.Configurations;

/// <summary>How the envelopes and the users of the list are stored: the domain has no attributes of the driver, the mapping is here. The enums are their names.</summary>
public static class IntegrationClassMaps
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(OutboundEnvelope)))
        {
            return;
        }

        MongoEntityMaps.RegisterBases<Guid>();
        BsonClassMap.RegisterClassMap<EnvelopeEvent>(map =>
        {
            map.MapProperty(entry => entry.At);
            map.MapProperty(entry => entry.Code);
            map.MapProperty(entry => entry.Text);
            map.MapProperty(entry => entry.Source);
            map.MapProperty(entry => entry.Note);
        });
        BsonClassMap.RegisterClassMap<OutboundEnvelope>(map =>
        {
            map.MapProperty(envelope => envelope.EnvelopeId);
            map.MapProperty(envelope => envelope.Kind).SetSerializer(new EnumSerializer<OutboundKind>(BsonType.String));
            map.MapProperty(envelope => envelope.State).SetSerializer(new EnumSerializer<OutboundState>(BsonType.String));
            map.MapProperty(envelope => envelope.TenantId);
            map.MapProperty(envelope => envelope.ObjectName);
            map.MapProperty(envelope => envelope.ContentLength);
            map.MapProperty(envelope => envelope.SenderAlias);
            map.MapProperty(envelope => envelope.SenderRegisterNumber);
            map.MapProperty(envelope => envelope.ReceiverAlias);
            map.MapProperty(envelope => envelope.ReceiverRegisterNumber);
            map.MapProperty(envelope => envelope.ReceiverTitle);
            map.MapProperty(envelope => envelope.InvoiceRecordId);
            map.MapProperty(envelope => envelope.InvoiceId);
            map.MapProperty(envelope => envelope.InvoiceNumber);
            map.MapProperty(envelope => envelope.QueueId);
            map.MapProperty(envelope => envelope.ReferenceEnvelopeId);
            map.MapProperty(envelope => envelope.AnswerUuid);
            map.MapProperty(envelope => envelope.StatusCode);
            map.MapProperty(envelope => envelope.StatusDescription);
            map.MapProperty(envelope => envelope.FailureStage);
            map.MapProperty(envelope => envelope.FailureReason);
            map.MapProperty(envelope => envelope.CreatedAt);
            map.MapProperty(envelope => envelope.StatusChangedAt);
            map.MapProperty(envelope => envelope.SendCount);
            map.MapProperty(envelope => envelope.LastSendAt);
            map.MapProperty(envelope => envelope.NextSendAt);
            map.MapProperty(envelope => envelope.LastError);
            map.MapProperty(envelope => envelope.History);
        });
        BsonClassMap.RegisterClassMap<InboundEnvelope>(map =>
        {
            map.MapProperty(envelope => envelope.EnvelopeId);
            map.MapProperty(envelope => envelope.State).SetSerializer(new EnumSerializer<InboundState>(BsonType.String));
            map.MapProperty(envelope => envelope.TenantId);
            map.MapProperty(envelope => envelope.ObjectName);
            map.MapProperty(envelope => envelope.ContentLength);
            map.MapProperty(envelope => envelope.ReceivedAt);
            map.MapProperty(envelope => envelope.ProcessedAt);
            map.MapProperty(envelope => envelope.StatusCode);
            map.MapProperty(envelope => envelope.StatusDescription);
            map.MapProperty(envelope => envelope.Attempts);
            map.MapProperty(envelope => envelope.DocumentType);
            map.MapProperty(envelope => envelope.SenderAlias);
            map.MapProperty(envelope => envelope.SenderRegisterNumber);
            map.MapProperty(envelope => envelope.SenderTitle);
            map.MapProperty(envelope => envelope.ReceiverAlias);
            map.MapProperty(envelope => envelope.ReceiverRegisterNumber);
            map.MapProperty(envelope => envelope.ReceiverTitle);
            map.MapProperty(envelope => envelope.EnvelopeCreatedAt);
            map.MapProperty(envelope => envelope.InvoiceUuid);
            map.MapProperty(envelope => envelope.InvoiceNumber);
            map.MapProperty(envelope => envelope.SupplierRegisterNumber);
            map.MapProperty(envelope => envelope.SupplierName);
            map.MapProperty(envelope => envelope.ReferenceEnvelopeId);
            map.MapProperty(envelope => envelope.AnswerOutboundId);
            map.MapProperty(envelope => envelope.CenterStatusCode);
            map.MapProperty(envelope => envelope.CenterStatusDescription);
            map.MapProperty(envelope => envelope.CenterStatusAt);
            map.MapProperty(envelope => envelope.CenterCheckedAt);
            map.MapProperty(envelope => envelope.History);
        });
        BsonClassMap.RegisterClassMap<ExchangeUser>(map =>
        {
            map.MapProperty(user => user.RegisterNumber);
            map.MapProperty(user => user.Title);
            map.MapProperty(user => user.PostBoxAliases);
            map.MapProperty(user => user.RefreshedAt);
        });
    }
}
