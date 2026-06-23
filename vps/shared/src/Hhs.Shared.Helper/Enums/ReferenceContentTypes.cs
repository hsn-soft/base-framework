using System.ComponentModel.DataAnnotations;

namespace Hhs.Shared.Helper.Enums;

public enum ReferenceContentTypes
{
    [Display(Name = "CUSTOMER_CONTENT")]
    CUSTOMER_CONTENT = 0,

    [Display(Name = "ANALYSIS_CONTENT")]
    ANALYSIS_CONTENT = 1
}