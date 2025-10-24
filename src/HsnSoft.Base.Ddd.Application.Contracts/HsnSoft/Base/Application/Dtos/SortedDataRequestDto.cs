using System;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public class SortedDataRequestDto : ISortedDataRequest
{
    public virtual string SortingText { get; set; }
}