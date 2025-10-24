using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HsnSoft.Base.Application.Dtos;

public interface ILimitedDataRequest
{
    int MaxResultCount { get; set; }
}

[Serializable]
public class LimitedDataRequestDto : ILimitedDataRequest, IValidatableObject
{
    public static int DefaultMaxResultCount { get; set; } = 10;

    public static int MaxMaxResultCount { get; set; } = 100;

    [Range(1, int.MaxValue)] public int MaxResultCount { get; set; } = DefaultMaxResultCount;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaxResultCount > MaxMaxResultCount)
        {
            yield return new ValidationResult("MaxResultCountExceededExceptionMessage", [nameof(MaxResultCount)]);
        }
    }
}

[Serializable]
public class SortedAndLimitedDataRequestDto : LimitedDataRequestDto, ISortedDataRequest
{
    public string SortingText { get; set; }
}

[Serializable]
public class SearchDataRequestDto : SortedAndLimitedDataRequestDto, ISearchDataRequest
{
    public virtual string SearchText { get; set; }
}

[Serializable]
public class PagedDataRequestDto : SearchDataRequestDto, IPagedDataRequest
{
    [Range(1, int.MaxValue)] public int PageNumber { get; set; }
}