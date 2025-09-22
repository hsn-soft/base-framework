namespace HsnSoft.Base.Domain.Models;

public class PagedQueryOptions<T> : OrderQueryOptions<T>
{
    public uint PageNumber { get; init; } = 1;
    public uint PageSize { get; init; } = 5;
}