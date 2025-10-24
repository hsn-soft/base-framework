using System;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public class PagedAndSortedDataRequestDto : PagedDataRequestDto, IPagedAndSortedDataRequest
{
    public virtual string SortingText { get; set; }
}