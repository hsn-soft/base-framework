using System.ComponentModel.DataAnnotations;

namespace HsnSoft.Base.Sample.Api.Domain.Enums;

public enum UserOperationStates
{
    [Display(Name = "None")] None = 0,

    [Display(Name = "Approved")] Approved = 11,

    [Display(Name = "Fail")] OperationFail = 51,

    [Display(Name = "Success")] OperationSuccess = 99
}