using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HsnSoft.Base.Validation;

public interface IHasValidationErrors
{
    IList<ValidationResult> ValidationErrors { get; }
}