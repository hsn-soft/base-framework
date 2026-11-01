using HsnSoft.Base;

namespace Hsx.ContentService.Domain.CustomerContentVisitDomain.Exceptions;

/// <summary>There is no such customer content visit: answered as 404.</summary>
public static class CustomerContentVisitNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"CustomerContentVisit {id} was not found.").WithKey("customer_content_visit_not_found", id);
}
