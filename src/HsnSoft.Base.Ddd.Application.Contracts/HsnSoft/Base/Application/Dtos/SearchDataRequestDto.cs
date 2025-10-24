using System;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public class SearchDataRequestDto : SearchLimitedDataRequestDto, ISearchDataRequest
{
    public virtual string SearchText { get; set; }
}