using HsnSoft.Base;

namespace Hsx.TextNormalizerService.Domain.CustomerContentNormalizedRequestDomain.Exceptions;

/// <summary>There is no such customer content normalized request: answered as 404.</summary>
public static class CustomerContentNormalizedRequestNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"CustomerContentNormalizedRequest {id} was not found.").WithKey("customer_content_normalized_request_not_found", id);
}
