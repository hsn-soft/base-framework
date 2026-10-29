using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Edx.InvoiceService.MongoDB.Configurations;

/// <summary>What the two kinds of invoice keep inside them: their history and the answers of the receiver.</summary>
internal static class InvoiceValueMaps
{
    public static void Register()
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(InvoiceHistoryEntry)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<InvoiceHistoryEntry>(map =>
        {
            map.MapProperty(entry => entry.Kind);
            map.MapProperty(entry => entry.Value);
            map.MapProperty(entry => entry.At);
            map.MapProperty(entry => entry.By);
            map.MapProperty(entry => entry.Note);
        });
        BsonClassMap.RegisterClassMap<InvoiceResponse>(map =>
        {
            map.MapProperty(response => response.Kind).SetSerializer(new EnumSerializer<InvoiceResponseKind>(BsonType.String));
            map.MapProperty(response => response.DocumentNumber);
            map.MapProperty(response => response.DocumentUuid);
            map.MapProperty(response => response.At);
            map.MapProperty(response => response.Note);
            map.MapProperty(response => response.EnvelopeId);
            map.MapProperty(response => response.EnvelopeState);
            map.MapProperty(response => response.EnvelopeStatusCode);
            map.MapProperty(response => response.EnvelopeStatusText);
            map.MapProperty(response => response.EnvelopeStatusAt);
        });
    }
}
