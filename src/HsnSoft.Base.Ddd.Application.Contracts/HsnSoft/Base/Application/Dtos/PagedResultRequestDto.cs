using System;
using System.ComponentModel.DataAnnotations;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public class PagedResultRequestDto : PagedLimitedResultRequestDto, IPagedResultRequest
{
    [Range(0, int.MaxValue)]
    public virtual int SkipCount { get; set; }
}
