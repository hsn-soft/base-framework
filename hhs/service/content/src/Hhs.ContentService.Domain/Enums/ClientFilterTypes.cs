using System.ComponentModel.DataAnnotations;

namespace Hhs.ContentService.Domain.Enums;

public enum ClientFilterTypes
{
    [Display(Name = "IncludeFilter")]
    IncludeFilter = 0,

    [Display(Name = "ExcludeFilter")]
    ExcludeFilter = 1
}