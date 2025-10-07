using System.ComponentModel.DataAnnotations;

namespace HsnSoft.Base.Domain.Models;

public class ListQueryOptions<T> : OrderQueryOptions<T>
{
    [Range(1, int.MaxValue)] public int? MaxResultCount { get; init; }
}