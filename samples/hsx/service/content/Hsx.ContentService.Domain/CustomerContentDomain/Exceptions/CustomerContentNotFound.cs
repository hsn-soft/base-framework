using HsnSoft.Base;

namespace Hsx.ContentService.Domain.CustomerContentDomain.Exceptions;

/// <summary>There is no such customer content: answered as 404.</summary>
public static class CustomerContentNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"CustomerContent {id} was not found.").WithKey("customer_content_not_found", id);
}
