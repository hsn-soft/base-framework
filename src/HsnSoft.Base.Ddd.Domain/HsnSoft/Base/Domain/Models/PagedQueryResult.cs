using System.Collections.Generic;

namespace HsnSoft.Base.Domain.Models;

public class PagedQueryResult<T>
{
    public List<T> Items { get; init; } = [];
    public uint TotalCount { get; init; }
    public uint PageNumber { get; init; }
    public uint PageSize { get; init; }
}