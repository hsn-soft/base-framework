namespace Edx.InvoiceService.Application.Importing;

/// <summary>What the invoice service accepts when it takes an invoice in (section <c>Import</c>).</summary>
public sealed class InvoiceImportOptions
{
    public const string SectionName = "Import";

    /// <summary>The most the file in a zip may weigh when unpacked, in bytes: a zip that is small and unpacks to gigabytes is refused.</summary>
    public long MaxUnpackedBytes { get; set; } = 524_288_000;
}
