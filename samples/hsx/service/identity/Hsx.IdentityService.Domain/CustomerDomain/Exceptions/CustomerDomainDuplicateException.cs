using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.CustomerDomain.Exceptions;

/// <summary>There is a customer with this domain already (a known error: the client is told, with the code <see cref="Code"/>).</summary>
public sealed class CustomerDomainDuplicateException : BusinessException
{
    public const string Code = "customer_domain_duplicate";

    public CustomerDomainDuplicateException(string value)
        : base(Code, $"A customer with the domain '{value}' exists.") => WithKey(Code, value);
}
