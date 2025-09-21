using System;

namespace HsnSoft.Base.MongoDB.Context;

public class MongoEntityEventArgs : EventArgs
{
    public MongoEntityEventState EventState { get; init; }
    public object EntryEntity { get; init; }
}