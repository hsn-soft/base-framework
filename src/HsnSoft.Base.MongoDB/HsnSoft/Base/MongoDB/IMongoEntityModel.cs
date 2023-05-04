using System;

namespace HsnSoft.Base.MongoDB;

public interface IMongoEntityModel
{
    Type EntityType { get; }

    string CollectionName { get; }
}