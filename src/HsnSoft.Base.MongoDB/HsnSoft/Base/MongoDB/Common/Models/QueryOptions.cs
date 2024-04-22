using System;

namespace HsnSoft.Base.MongoDB.Common.Models;

public class QueryOptions
{
    public TimeSpan MaxTime { get; set; }
    public TimeSpan MaxAwaitTime { get; set; }
}