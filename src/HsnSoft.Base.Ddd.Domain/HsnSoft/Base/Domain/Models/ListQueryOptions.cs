namespace HsnSoft.Base.Domain.Models;

public class ListQueryOptions<T> : OrderQueryOptions<T>
{
    public uint? ListLength { get; init; }
}