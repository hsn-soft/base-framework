using System.ComponentModel.DataAnnotations;

namespace HsnSoft.Base.Domain.Models;

public class PagedQueryOptions<T> : OrderQueryOptions<T>
{
    [Range(1, int.MaxValue)] public int ResultPageNumber { get; init; } = 1;

    [Range(1, int.MaxValue)] public int MaxResultCount { get; init; } = 5;
}