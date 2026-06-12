using System.ComponentModel.DataAnnotations;

namespace Hhs.VideoGeneratorService.Domain.Enums;

public enum VideoGenerationProviderTypes
{
    [Display(Name = "COLOSSYAN_AI")]
    COLOSSYAN_AI = 0,

    [Display(Name = "YEPIC_AI")]
    YEPIC_AI = 1,

    [Display(Name = "DID_AI")]
    DID_AI = 2,

    [Display(Name = "HEYGEN_AI")]
    HEYGEN_AI = 3,

    [Display(Name = "CREATOMATE")]
    CREATOMATE = 4
}