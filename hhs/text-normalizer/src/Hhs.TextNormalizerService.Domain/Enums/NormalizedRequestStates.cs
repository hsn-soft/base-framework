using System.ComponentModel.DataAnnotations;

namespace Hhs.TextNormalizerService.Domain.Enums;

public enum NormalizedRequestStates
{
    [Display(Name = "CreatedWaitForScraping")]
    CreatedWaitForScraping = 0,

    [Display(Name = "ContentScrappedWaitForOutline")]
    ContentScrappedWaitForOutline = 11,

    [Display(Name = "Fail")]
    OperationFail = 51,

    [Display(Name = "Success")]
    OperationSuccess = 99
}