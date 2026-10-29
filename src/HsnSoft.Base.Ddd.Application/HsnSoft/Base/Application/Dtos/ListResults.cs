using System;
using System.Collections.Generic;

namespace HsnSoft.Base.Application.Dtos;

public interface IListDataResult<T>
{
    IReadOnlyList<T> Items { get; set; }
}

/// <summary>A list that is not paged.</summary>
public class ListDataResultDto<T> : IListDataResult<T>
{
    public ListDataResultDto()
    {
    }

    public ListDataResultDto(IReadOnlyList<T> items) => Items = items;

    public IReadOnlyList<T> Items { get; set => field = value ?? []; } = [];
}

/// <summary>A page of a list, with how many there are in all and where the page is.</summary>
public class PagedDataResultDto<T> : ListDataResultDto<T>
{
    public PagedDataResultDto()
    {
    }

    public PagedDataResultDto(long dataTotalCount, int pageNumber, int pageSize, IReadOnlyList<T> pageDataItems)
        : base(pageDataItems)
    {
        DataTotalCount = Math.Max(dataTotalCount, 0);
        pageSize = Math.Max(pageSize, 1);
        int skip = (Math.Max(pageNumber, 1) - 1) * pageSize;
        if (DataTotalCount <= skip)
        {
            return;
        }

        PageTotalCount = (int)Math.Ceiling((double)DataTotalCount / pageSize);
        ResultDataStartNumber = skip + 1;
        ResultDataEndNumber = skip + Items.Count;
    }

    /// <summary>How many there are in all.</summary>
    public long DataTotalCount { get; set; }

    /// <summary>How many pages there are.</summary>
    public int PageTotalCount { get; set; }

    /// <summary>The number of the first item of this page among all (from 1); 0 when the page is empty.</summary>
    public int ResultDataStartNumber { get; set; }

    /// <summary>The number of the last item of this page among all.</summary>
    public int ResultDataEndNumber { get; set; }
}
