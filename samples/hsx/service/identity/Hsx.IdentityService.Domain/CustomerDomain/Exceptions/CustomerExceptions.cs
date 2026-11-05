using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.CustomerDomain.Exceptions;

/// <summary>There is no such customer: answered as 404.</summary>
public static class CustomerNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Customer {id} was not found.").WithKey("customer_not_found", id);
}

/// <summary>There is a customer with this domain already (a known error: the client is told, with the code <see cref="ErrorKey"/>).</summary>
public sealed class CustomerDomainDuplicateException : BusinessException
{
    public const string ErrorKey = "customer_domain_duplicate";

    public CustomerDomainDuplicateException(string value)
        : base(ErrorKey, $"A customer with the domain '{value}' exists.") => WithKey(ErrorKey, value);
}
