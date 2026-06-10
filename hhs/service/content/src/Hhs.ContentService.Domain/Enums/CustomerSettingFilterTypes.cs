using System.ComponentModel.DataAnnotations;

namespace Hhs.ContentService.Domain.Enums;

public enum CustomerSettingFilterTypes
{
    [Display(Name = "IncludeFilter")]
    IncludeFilter = 0,

    [Display(Name = "ExcludeFilter")]
    ExcludeFilter = 1
}