using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.Logging;

public interface IInitLogger<out T> : ILogger<T>
{
    public List<BaseInitLogEntry> Entries { get; }
}