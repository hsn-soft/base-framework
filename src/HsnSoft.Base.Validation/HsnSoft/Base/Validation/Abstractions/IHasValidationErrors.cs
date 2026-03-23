using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HsnSoft.Base.Validation.Abstractions;

public interface IHasValidationErrors
{
    IList<ValidationResult> ValidationErrors { get; }
}