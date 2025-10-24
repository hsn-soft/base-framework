using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public class PagedDataResultDto<T> : ListDataResultDto<T>, IPagedDataResult<T>
{
    [Range(0, long.MaxValue)] public long DataTotalCount { get; set; }
    [Range(0, int.MaxValue)] public int PageTotalCount { get; }
    [Range(0, int.MaxValue)] public int ResultDataStartNumber { get; }
    [Range(0, int.MaxValue)] public int ResultDataEndNumber { get; }

    public PagedDataResultDto()
    {
    }

    public PagedDataResultDto(long dataTotalCount, int pageNumber, int pageSize, IReadOnlyList<T> pageDataItems) : base(pageDataItems)
    {
        DataTotalCount = dataTotalCount;

        if (dataTotalCount < 0) pageNumber = 0;
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;

        int skip = (pageNumber - 1) * pageSize;
        if (dataTotalCount <= skip)
        {
            return;
        }

        PageTotalCount = (int)Math.Ceiling((double)dataTotalCount / pageSize);
        ResultDataStartNumber = skip + 1;
        ResultDataEndNumber = skip + Items.Count;
    }
}