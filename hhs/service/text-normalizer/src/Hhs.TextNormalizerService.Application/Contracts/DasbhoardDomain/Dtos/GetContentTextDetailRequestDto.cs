using System.ComponentModel.DataAnnotations;

namespace Hhs.TextNormalizerService.Application.Contracts.DasbhoardDomain.Dtos;

public sealed class GetContentTextDetailRequestDto
{
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; set; }
    public string SearchKeyword { get; set; }
}

