using System;

namespace HsnSoft.Base.MongoDB.Context;

public class MongoEntityEventArgs : EventArgs
{
    public MongoEntityEventState EventState { get; set; }
    public object EntryEntity { get; set; }
}