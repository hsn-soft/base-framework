using System.ComponentModel.DataAnnotations;

namespace Hhs.TextNormalizerService.Domain.Enums;

public enum TextNormalizeProviderTypes
{
    [Display(Name = "OPEN_AI")]
    OPEN_AI = 0
}