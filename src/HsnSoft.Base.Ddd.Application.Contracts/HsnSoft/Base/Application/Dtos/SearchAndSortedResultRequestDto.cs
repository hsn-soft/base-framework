using System;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public class SearchAndSortedResultRequestDto : SearchResultRequestDto, ISearchAndSortedResultRequest
{
    public virtual string Sorting { get; set; }
}