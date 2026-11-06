using System.ComponentModel.DataAnnotations;
using HsnSoft.Base;
using Hsx.ContentService.Domain.CustomerContentVisitDomain.Consts;

namespace Hsx.ContentService.Application.Contracts.CustomerContentVisitDomain.Dtos.Submits;

public sealed class CustomerContentVisitCreateDto
{
    [Required]
    [StringLength(CustomerContentVisitConsts.ScopeKeyMaxLength)]
    public string? ScopeKey { get; set; }

    [Required]
    [NotDefault]
    public Guid? CustomerContentId { get; set; }

    [Required]
    [StringLength(CustomerContentVisitConsts.VisitResponseMaxLength)]
    public string? VisitResponse { get; set; }
}
