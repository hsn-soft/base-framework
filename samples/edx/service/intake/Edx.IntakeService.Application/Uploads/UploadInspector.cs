using System.IO.Compression;
using System.Xml;
using Edx.IntakeService.Application.Storage;
using Edx.IntakeService.Domain.QueueDomain.Exceptions;
using Edx.Shared.Contracts.Invoicing;
using Microsoft.Extensions.Options;

namespace Edx.IntakeService.Application.Uploads;

/// <summary>What is in a zip that was sent: the name of its one file and what it weighs when unpacked.</summary>
public sealed record UploadInspection(string EntryName, long UnpackedBytes);

/// <summary>
/// Looks into a zip a customer sent before anything of it is kept (the service answers at once, so a sender learns of a mistake with the same call): it must be a zip, hold **one** file, which is of the type the sender said it is (<c>.xml</c>, <c>.csv</c>, <c>.txt</c>,
/// <c>.xls</c> or <c>.xlsx</c>), not too heavy when unpacked, with a name that stays where it is; an XML file must be well formed. Nothing is unpacked to disk and nothing is kept: the zip is stored as it came, and what is in the file (its columns, its schema) is for the invoice service to check.
/// </summary>
public sealed class UploadInspector(IOptions<IntakeOptions> options)
{
    private static readonly Dictionary<InvoiceSourceType, string[]> s_extensions = new()
    {
        [InvoiceSourceType.Xml] = [".xml"],
        [InvoiceSourceType.Csv] = [".csv"],
        [InvoiceSourceType.Txt] = [".txt"],
        [InvoiceSourceType.Xls] = [".xls", ".xlsx"],
    };

    /// <summary>The one file of the zip, or an <see cref="InvalidUploadException"/> that says what is wrong. <paramref name="content"/> must be seekable (it is read from its start).</summary>
    public UploadInspection Inspect(Stream content, InvoiceSourceType sourceType)
    {
        ArgumentNullException.ThrowIfNull(content);

        string[] extensions = s_extensions.TryGetValue(sourceType, out string[]? known) ? known : throw new InvalidUploadException(InvalidUploadException.TypeMismatch, $"A zip cannot hold a '{sourceType}'.", sourceType.ToString());
        long maxUnpacked = options.Value.MaxUnpackedBytes;
        content.Position = 0;
        try
        {
            using ZipArchive archive = new(content, ZipArchiveMode.Read, leaveOpen: true);
            List<ZipArchiveEntry> files = [.. archive.Entries.Where(entry => !(entry.FullName.EndsWith('/') && entry.Length == 0))];
            if (files.Count != 1)
            {
                throw new InvalidUploadException(InvalidUploadException.NotOneFile, $"The zip must hold one file, it holds {files.Count}.", files.Count);
            }

            ZipArchiveEntry entry = files[0];
            string name = entry.FullName.Replace('\\', '/');
            if (Path.IsPathRooted(name) || name.Split('/').Contains("..") || name.Contains('\0', StringComparison.Ordinal))
            {
                throw new InvalidUploadException(InvalidUploadException.EntryNameUnsafe, $"The name '{entry.FullName}' in the zip is not allowed.", entry.FullName);
            }

            if (!extensions.Contains(Path.GetExtension(entry.Name), StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidUploadException(InvalidUploadException.TypeMismatch, $"The file '{entry.FullName}' is not a {sourceType} file ({string.Join(", ", extensions)}).", entry.FullName, sourceType.ToString());
            }

            if (entry.Length > maxUnpacked)
            {
                throw new InvalidUploadException(InvalidUploadException.TooLarge, $"The file weighs more than {maxUnpacked} bytes when unpacked.", maxUnpacked);
            }

            if (sourceType == InvoiceSourceType.Xml)
            {
                CheckWellFormed(entry, maxUnpacked);
            }

            return new UploadInspection(entry.FullName, entry.Length);
        }
        catch (InvalidDataException)
        {
            throw new InvalidUploadException(InvalidUploadException.NotAZip, "The file is not a readable zip.");
        }
        finally
        {
            content.Position = 0;
        }
    }

    // DTDs and external entities are off, so a document cannot pull in a file or a host, nor blow up on entities; the size read is limited too, whatever the entry says its size is
    private static void CheckWellFormed(ZipArchiveEntry entry, long maxCharacters)
    {
        XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = maxCharacters };
        try
        {
            using Stream stream = entry.Open();
            using XmlReader reader = XmlReader.Create(stream, settings);
            while (reader.Read())
            {
                // reading is the check
            }
        }
        catch (XmlException exception)
        {
            throw new InvalidUploadException(InvalidUploadException.XmlNotWellFormed, $"The file '{entry.FullName}' is not well formed XML: {exception.Message}", entry.FullName);
        }
    }
}
