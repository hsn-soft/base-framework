using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.CompanyDomain.Exceptions;

/// <summary>There is no such company: answered as 404.</summary>
public static class CompanyNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Company {id} was not found.").WithKey("company_not_found", id);
}
