using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.CompanyDomain.Exceptions;

/// <summary>There is no such company: answered as 404.</summary>
public static class CompanyNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Company {id} was not found.").WithKey("company_not_found", id);
}

/// <summary>There is a company with this name already (a known error: the client is told, with the code <see cref="ErrorKey"/>).</summary>
public sealed class CompanyNameDuplicateException : BusinessException
{
    public const string ErrorKey = "company_name_duplicate";

    public CompanyNameDuplicateException(string value)
        : base(ErrorKey, $"A company with the name '{value}' exists.") => WithKey(ErrorKey, value);
}
