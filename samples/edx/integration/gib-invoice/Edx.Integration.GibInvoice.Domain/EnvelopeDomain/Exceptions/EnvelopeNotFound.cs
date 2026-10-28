using HsnSoft.Base;

namespace Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Exceptions;

/// <summary>There is no such envelope (or it is another tenant's): answered as 404.</summary>
public static class EnvelopeNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"Envelope {id} was not found.").WithKey("envelope_not_found", id);
}
