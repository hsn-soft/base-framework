using System;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public class SearchResultRequestDto : SearchLimitedResultRequestDto, ISearchResultRequest
{
    public virtual string SearchText { get; set; }
}