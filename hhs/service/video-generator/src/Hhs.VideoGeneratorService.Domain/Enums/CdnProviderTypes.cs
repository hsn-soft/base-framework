using System.ComponentModel.DataAnnotations;

namespace Hhs.VideoGeneratorService.Domain.Enums;

public enum CdnProviderTypes
{
    [Display(Name = "BUNNY_CDN_SELF_STORAGE")]
    BUNNY_CDN_SELF_STORAGE = 0,

    [Display(Name = "BUNNY_CDN_S3_STORAGE")]
    BUNNY_CDN_S3_STORAGE = 1
}