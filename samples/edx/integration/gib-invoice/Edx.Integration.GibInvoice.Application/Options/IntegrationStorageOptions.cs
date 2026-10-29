namespace Edx.Integration.GibInvoice.Application.Options;

/// <summary>Where the service keeps its files (section <c>Storage</c>): one container of the blob store, with the folders <c>outbound/</c> (the envelopes it hands to the center), <c>inbound/</c> (the ones it takes) and <c>invoices/</c> (the invoices that came in).</summary>
public sealed class IntegrationStorageOptions
{
    public const string SectionName = "Storage";

    public string Container { get; set; } = "edx-integration-gib-invoice";
}
