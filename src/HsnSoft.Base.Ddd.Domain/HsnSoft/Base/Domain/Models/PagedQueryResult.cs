using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HsnSoft.Base.Domain.Models;

public class PagedQueryResult<T>
{
    public List<T> Items { get; init; } = [];
    [Range(0, long.MaxValue)] public long TotalCount { get; init; }
}