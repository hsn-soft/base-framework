using System;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public class FilterResultRequestDto : FilterLimitedResultRequestDto, IFilterResultRequest
{
    public virtual string FilterText { get; set; }
}