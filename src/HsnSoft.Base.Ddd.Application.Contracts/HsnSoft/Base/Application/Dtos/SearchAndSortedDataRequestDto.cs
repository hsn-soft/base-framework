using System;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public class SearchAndSortedDataRequestDto : SearchDataRequestDto, ISearchAndSortedDataRequest
{
    public virtual string SortingText { get; set; }
}