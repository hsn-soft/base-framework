using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace HsnSoft.Base.MongoDB.Serializers;

public class DynamicSerializer : SerializerBase<dynamic>
{
    public override dynamic Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var serializer = BsonSerializer.LookupSerializer(typeof(BsonDocument));
        var document = serializer.Deserialize(context, args);
        var bsonDocument = document.ToBsonDocument();
        var result = bsonDocument.ToJson();
        return JsonConvert.DeserializeObject<dynamic>(result);
    }


    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, dynamic value)
    {
        var jsonDocument = JsonConvert.SerializeObject(value);
        var bsonDocument = BsonSerializer.Deserialize<BsonDocument>(jsonDocument);
        var serializer = BsonSerializer.LookupSerializer(typeof(BsonDocument));
        serializer.Serialize(context, args, bsonDocument);
    }
}