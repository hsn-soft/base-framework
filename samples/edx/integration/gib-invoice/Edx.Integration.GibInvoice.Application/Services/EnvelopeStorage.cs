using Edx.Integration.GibInvoice.Application.Options;
using HsnSoft.Base.BlobStoring;
using Microsoft.Extensions.Options;

namespace Edx.Integration.GibInvoice.Application.Services;

/// <summary>Where the zips and the invoices are kept: one container of the blob store (<see cref="IntegrationStorageOptions"/>), the folders of the tenant, the kind and the minute.</summary>
public sealed class EnvelopeStorage(IBlobStore blobs, IOptions<IntegrationStorageOptions> options)
{
    public string Container => options.Value.Container;

    /// <summary>
    /// Where the zips are kept (nothing but zips): <c>{tenant}/{kind}/{year}/{month}{day}/{hour}{minute}/{name}_{envelope}.zip</c> (UTC), the tenant first as in the firm invoice integration's container (<c>unassigned</c> for an envelope no tenant owns).
    /// The kinds: <c>sales</c> (an envelope of our sales invoices, to the center), <c>sales_answer</c> (the answers of the receivers to them, from the center), <c>purchase</c> (the invoices that came in for us), <c>purchase_answer</c> (our answers to them) and <c>system</c> (system envelopes: <c>inbound_</c> and <c>outbound_</c>).
    /// </summary>
    public static string ZipName(Guid? tenantId, string kind, string name, DateTimeOffset at, string envelopeId)
    {
        DateTime utc = at.UtcDateTime;
        string tenant = tenantId is { } id ? id.ToString("N") : "unassigned";
        return $"{tenant}/{kind}/{utc:yyyy}/{utc:MMdd}/{utc:HHmm}/{name}_{Compact(envelopeId)}.zip";
    }

    public string SalesName(Guid tenantId, DateTimeOffset at, string envelopeId) => ZipName(tenantId, "sales", "sales", at, envelopeId);

    public string SalesAnswerName(Guid? tenantId, DateTimeOffset at, string envelopeId) => ZipName(tenantId, "sales_answer", "sales_answer", at, envelopeId);

    public string PurchaseName(Guid? tenantId, DateTimeOffset at, string envelopeId) => ZipName(tenantId, "purchase", "purchase", at, envelopeId);

    public string PurchaseAnswerName(Guid tenantId, DateTimeOffset at, string envelopeId) => ZipName(tenantId, "purchase_answer", "purchase_answer", at, envelopeId);

    public string SystemInboundName(Guid? tenantId, DateTimeOffset at, string envelopeId) => ZipName(tenantId, "system", "inbound", at, envelopeId);

    public string SystemOutboundName(Guid tenantId, DateTimeOffset at, string envelopeId) => ZipName(tenantId, "system", "outbound", at, envelopeId);

    // the ids in the names of the objects are the uuids without dashes
    private static string Compact(string id) => Guid.TryParse(id, out Guid guid) ? guid.ToString("N") : id;

    public async Task SaveAsync(string objectName, byte[] content, string contentType, CancellationToken cancellationToken)
    {
        using MemoryStream stream = new(content, writable: false);
        await blobs.SaveAsync(Container, objectName, stream, contentType, cancellationToken).ConfigureAwait(false);
    }

    public Task<byte[]?> ReadAsync(string objectName, CancellationToken cancellationToken) => ReadAsync(Container, objectName, cancellationToken);

    /// <summary>A file of a container (the signed invoice is in the container of the invoice service).</summary>
    public async Task<byte[]?> ReadAsync(string container, string objectName, CancellationToken cancellationToken)
    {
        Stream? stream = await blobs.OpenReadAsync(container, objectName, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        await using (stream.ConfigureAwait(false))
        {
            using MemoryStream copy = new();
            await stream.CopyToAsync(copy, cancellationToken).ConfigureAwait(false);
            return copy.ToArray();
        }
    }
}
