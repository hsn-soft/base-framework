using System;

namespace HsnSoft.Base.MongoDBOld;

public class QueryOptions
{
    public TimeSpan MaxTime { get; set; }
    public TimeSpan MaxAwaitTime { get; set; }
}