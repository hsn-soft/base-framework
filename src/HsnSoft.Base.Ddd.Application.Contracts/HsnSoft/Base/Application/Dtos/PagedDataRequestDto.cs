using System;
using System.ComponentModel.DataAnnotations;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public class PagedDataRequestDto : PagedLimitedDataRequestDto, IPagedDataRequest
{
    [Range(1, int.MaxValue)]
    public virtual int ResultPageNumber { get; set; }
}
