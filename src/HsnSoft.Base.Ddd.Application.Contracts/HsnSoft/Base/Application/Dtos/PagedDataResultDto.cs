using System;
using System.Collections.Generic;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public class PagedDataResultDto<T> : ListDataResultDto<T>, IPagedDataResult<T>
{
    public long TotalCount { get; set; }

    public int ResultPageNumber { get; set; }
    public int MaxResultCount { get; set; }

    public PagedDataResultDto()
    {
    }

    public PagedDataResultDto(long totalCount, IReadOnlyList<T> items)
        : base(items)
    {
        TotalCount = totalCount;
    }
}