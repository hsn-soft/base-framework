using System.ComponentModel.DataAnnotations;

namespace Hhs.VideoGeneratorService.Domain.Enums;

public enum VideoFormatTypes
{
    [Display(Name = "Unknown")]
    Unknown = 0,

    [Display(Name = "mp4")]
    mp4 = 1
}