using System.ComponentModel.DataAnnotations;

namespace Hhs.ContentService.Domain.Enums;

public enum VideoGenerationTypes
{
    [Display(Name = "DirectVideoGeneration")]
    DirectVideoGeneration = 0,

    [Display(Name = "TrendVideoGeneration")]
    TrendVideoGeneration = 1,

    [Display(Name = "AnalysisVideoGeneration")]
    AnalysisVideoGeneration = 2
}