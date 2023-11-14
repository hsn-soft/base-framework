using System;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public class FilterAndSortedResultRequestDto : FilterResultRequestDto, IFilterAndSortedResultRequest
{
    public virtual string Sorting { get; set; }
}