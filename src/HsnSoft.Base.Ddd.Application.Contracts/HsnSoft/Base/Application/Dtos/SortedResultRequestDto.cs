using System;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public class SortedResultRequestDto : ISortedResultRequest
{
    public virtual string SortingText { get; set; }
}