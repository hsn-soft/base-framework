using System.ComponentModel.DataAnnotations;
using Edx.IntakeService.Application.Contracts.IntakeDomain.Dtos.Submits;
using HsnSoft.Base;

namespace Edx.IntakeService.Http.Host.Validation;

/// <summary>
/// Checks a model the way a REST request is checked (<c>[ApiController]</c> reads the attributes of the class and answers 400), for the two entry points that have no such check: gRPC and WCF build the class themselves from
/// what came on the wire, so the attributes are read here before the application service is called.
/// </summary>
public static class ModelValidation
{
    public const string Code = "model_validation_failed";

    /// <summary>Throws a <see cref="BusinessException"/> that lists what is wrong with the model and its lines.</summary>
    public static void Ensure(InvoiceSubmissionDto model)
    {
        ArgumentNullException.ThrowIfNull(model);

        List<ValidationResult> problems = [];
        Validator.TryValidateObject(model, new ValidationContext(model), problems, validateAllProperties: true);
        for (int index = 0; index < model.Lines.Count; index++)
        {
            InvoiceLineDto line = model.Lines[index];
            List<ValidationResult> lineProblems = [];
            Validator.TryValidateObject(line, new ValidationContext(line), lineProblems, validateAllProperties: true);
            problems.AddRange(lineProblems.Select(problem => new ValidationResult($"Lines[{index}]: {problem.ErrorMessage}", problem.MemberNames)));
        }

        if (problems.Count > 0)
        {
            throw new BusinessException(Code, string.Join(" ", problems.Select(problem => problem.ErrorMessage))).WithKey(Code);
        }
    }
}
