namespace Edx.InvoiceService.Application.Storage;

/// <summary>Where the invoice service keeps its files (section <c>Storage</c>): one container of the blob store, with the folders <c>uploads/</c>, <c>extracted/</c>, <c>draft/</c> and <c>signed/</c> in it, each with a folder of the tenant.</summary>
public sealed class InvoiceStorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>The container (bucket) of the invoice service.</summary>
    public string Container { get; set; } = "edx-invoice";
}
