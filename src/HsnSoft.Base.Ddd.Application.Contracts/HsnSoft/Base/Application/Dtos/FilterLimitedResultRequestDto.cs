using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HsnSoft.Base.Application.Dtos;

[Serializable]
public class FilterLimitedResultRequestDto : ILimitedResultRequest, IValidatableObject
{
    public static int DefaultMaxResultCount { get; set; } = 10;

    public static int MaxMaxResultCount { get; set; } = 100;

    [Range(1, int.MaxValue)]
    public virtual int MaxResultCount { get; set; } = DefaultMaxResultCount;

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaxResultCount > MaxMaxResultCount)
        {
            yield return new ValidationResult("MaxResultCountExceededExceptionMessage", new[] { nameof(MaxResultCount) });
        }
    }
}