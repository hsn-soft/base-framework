using System.Collections.Generic;

namespace HsnSoft.Base.Domain.Models;

public class PaginationResult<T>
{
    public List<T> Items { get; set; } = [];
    public uint TotalCount { get; set; }
    public uint PageNumber { get; set; }
    public uint PageSize { get; set; }
}