using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Application.Dtos;

/// <summary>How many a list may have at most. Section <c>Paging</c>.</summary>
public sealed class PagingOptions
{
    public const string SectionName = "Paging";

    /// <summary>The most a client may ask for in one list or page. Default 100.</summary>
    public int MaxResultCountLimit { get; set; } = 100;
}

public interface ILimitedDataRequest
{
    int MaxResultCount { get; set; }
}

public interface ISortedDataRequest : ILimitedDataRequest
{
    string? SortingText { get; set; }
}

public interface ISearchDataRequest : ISortedDataRequest
{
    string? SearchText { get; set; }
}

public interface IPagedDataRequest : ISearchDataRequest
{
    int PageNumber { get; set; }
}

/// <summary>A list with at most <see cref="MaxResultCount"/> items (10 unless the client says; never more than <see cref="PagingOptions.MaxResultCountLimit"/>).</summary>
public class LimitedDataRequestDto : ILimitedDataRequest, IValidatableObject
{
    public const int DefaultMaxResultCount = 10;

    [Range(1, int.MaxValue)]
    public int MaxResultCount { get; set; } = DefaultMaxResultCount;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        int limit = (validationContext.GetService(typeof(IOptions<PagingOptions>)) as IOptions<PagingOptions>)?.Value.MaxResultCountLimit ?? new PagingOptions().MaxResultCountLimit;
        if (MaxResultCount > limit)
        {
            yield return new ValidationResult($"MaxResultCount is at most {limit}.", [nameof(MaxResultCount)]);
        }
    }
}

/// <summary>A list that is sorted: <c>"name asc, price desc"</c>, by the fields the service offers.</summary>
public class SortedAndLimitedDataRequestDto : LimitedDataRequestDto, ISortedDataRequest
{
    [StringLength(200)]
    public string? SortingText { get; set; }
}

/// <summary>A list that is searched by a piece of text.</summary>
public class SearchDataRequestDto : SortedAndLimitedDataRequestDto, ISearchDataRequest
{
    [StringLength(100)]
    public virtual string? SearchText { get; set; }
}

/// <summary>A page of a list: the page counts from 1, and <see cref="LimitedDataRequestDto.MaxResultCount"/> is how many a page has.</summary>
public class PagedDataRequestDto : SearchDataRequestDto, IPagedDataRequest
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;
}
