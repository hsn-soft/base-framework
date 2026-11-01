using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.CompanyDomain.Exceptions;

/// <summary>There is a company with this name already (a known error: the client is told, with the code <see cref="Code"/>).</summary>
public sealed class CompanyNameDuplicateException : BusinessException
{
    public const string Code = "company_name_duplicate";

    public CompanyNameDuplicateException(string value)
        : base(Code, $"A company with the name '{value}' exists.") => WithKey(Code, value);
}
