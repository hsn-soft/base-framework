using System;

namespace HsnSoft.Base.MongoDB;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false)]
public class MongoCollectionAttribute : Attribute
{
    public string CollectionName { get; set; }

    public MongoCollectionAttribute()
    {
    }

    public MongoCollectionAttribute(string collectionName)
    {
        CollectionName = collectionName;
    }
}


// [AttributeUsage(AttributeTargets.Class, Inherited = false)]
// public class BsonCollectionAttribute : Attribute
// {
//     public string CollectionName { get; }
//
//     public BsonCollectionAttribute(string collectionName)
//     {
//         CollectionName = collectionName;
//     }
// }