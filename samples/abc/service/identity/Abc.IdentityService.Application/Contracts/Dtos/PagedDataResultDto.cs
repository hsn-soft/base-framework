namespace Abc.IdentityService.Application.Contracts.Dtos;

/// <summary>A page of a list, with how many there are in all.</summary>
public sealed class PagedDataResultDto<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public int TotalCount { get; init; }

    public int Skip { get; init; }

    public int Take { get; init; }
}
