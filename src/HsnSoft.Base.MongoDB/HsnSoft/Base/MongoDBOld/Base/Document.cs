using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HsnSoft.Base.MongoDBOld.Base;

public abstract class FullAuditDocument : BaseDocument, IFullAuditDocument
{
    public bool IsDeleted { get; set; }
    public DateTime CreationTime { get; set; }
    public string CreatorId { get; set; }
    public DateTime? LastModificationTime { get; set; }
    public string LastModifierId { get; set; }
}

public abstract class BaseDocument : IBaseDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    protected List<string> GetPropertiesNameList()
    {
        return GetType().GetProperties().Select(prop => prop.Name).ToList();
    }
}