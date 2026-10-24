using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Agt.Scheduler.Host;

/// <summary>A document of the watched collection. Other applications write them; the agent only reads the id and the creation time.</summary>
[BsonIgnoreExtraElements]
public sealed class SampleRecord
{
    [BsonId]
    public ObjectId Id { get; set; }

    // the element name is part of the contract with the applications that write the documents
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }
}
