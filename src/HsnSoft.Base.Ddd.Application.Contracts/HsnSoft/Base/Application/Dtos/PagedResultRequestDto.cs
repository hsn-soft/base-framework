using System;
using System.ComponentModel.DataAnnotations;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public class PagedResultRequestDto : PagedLimitedResultRequestDto, IPagedResultRequest
{
    [Range(1, int.MaxValue)]
    public virtual int ResultPageNumber { get; set; } = 1;
}
