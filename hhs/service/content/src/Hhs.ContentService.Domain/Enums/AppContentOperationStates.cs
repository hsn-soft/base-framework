using System.ComponentModel.DataAnnotations;

namespace Hhs.ContentService.Domain.Enums;

public enum AppContentOperationStates
{
    [Display(Name = "CreatedWaitForNormalize")]
    CreatedWaitForNormalize = 0,

    [Display(Name = "NormalizedWaitForVideoGenerationApprove")]
    NormalizedWaitForVideoGenerationApprove = 11,

    [Display(Name = "VideoGenerationApprovedWaitForGenerationResults")]
    VideoGenerationApprovedWaitForGenerationResults = 21,

    [Display(Name = "Fail")]
    OperationFail = 51,

    [Display(Name = "VideoGenerationRejectedReturnAnalysisVideo")]
    VideoGenerationRejectedReturnAnalysisVideo = 91,

    [Display(Name = "Success")]
    OperationSuccess = 99
}