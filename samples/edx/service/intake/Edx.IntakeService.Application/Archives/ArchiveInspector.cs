using System.IO.Compression;
using System.Xml;
using Edx.IntakeService.Application.Storage;
using Edx.IntakeService.Domain.StoredFileDomain.Entities;
using Edx.IntakeService.Domain.StoredFileDomain.Exceptions;
using Microsoft.Extensions.Options;

namespace Edx.IntakeService.Application.Archives;

/// <summary>
/// Looks into a zip a customer sent before anything of it is kept: it must be a zip, not empty, not too many files, not too heavy when unpacked, and hold only what the endpoint takes (XML files that are well formed, or
/// text, CSV and Excel files). Nothing is unpacked to disk: the archive is kept as it came, so a name inside it that tries to leave a folder (<c>../x</c>) can do no harm, but it is refused anyway, as it says the sender is up to something.
/// </summary>
public sealed class ArchiveInspector(IOptions<IntakeOptions> options)
{
    /// <summary>The files of the archive, or an <see cref="InvalidArchiveException"/> that says what is wrong. <paramref name="content"/> must be seekable (it is read from its start).</summary>
    public IReadOnlyList<ArchiveEntry> Inspect(Stream content, ArchiveKind kind)
    {
        ArgumentNullException.ThrowIfNull(content);

        IntakeOptions limits = options.Value;
        content.Position = 0;
        try
        {
            using ZipArchive archive = new(content, ZipArchiveMode.Read, leaveOpen: true);
            List<ArchiveEntry> entries = [];
            long unpacked = 0;
            foreach (ZipArchiveEntry entry in archive.Entries.Where(entry => !IsFolder(entry)))
            {
                if (entries.Count >= limits.MaxArchiveEntries)
                {
                    throw new InvalidArchiveException(InvalidArchiveException.TooManyEntries, $"An archive may hold at most {limits.MaxArchiveEntries} files.", limits.MaxArchiveEntries);
                }

                CheckName(entry);
                CheckExtension(entry, kind, limits);

                unpacked += entry.Length;
                if (unpacked > limits.MaxUnpackedBytes)
                {
                    throw new InvalidArchiveException(InvalidArchiveException.TooLarge, $"The files of an archive may weigh at most {limits.MaxUnpackedBytes} bytes when unpacked.", limits.MaxUnpackedBytes);
                }

                if (kind == ArchiveKind.Xml)
                {
                    CheckWellFormed(entry, limits.MaxUnpackedBytes);
                }

                entries.Add(ArchiveEntry.Of(entry.FullName, entry.Length));
            }

            return entries.Count == 0
                ? throw new InvalidArchiveException(InvalidArchiveException.Empty, "The archive holds no file.")
                : entries;
        }
        catch (InvalidDataException exception)
        {
            throw new InvalidArchiveException(InvalidArchiveException.NotAZip, "The file is not a readable zip archive.") { Data = { ["reason"] = exception.Message } };
        }
        finally
        {
            content.Position = 0;
        }
    }

    private static bool IsFolder(ZipArchiveEntry entry) => entry.FullName.EndsWith('/') && entry.Length == 0;

    // a name that is rooted, climbs out of its folder or holds a character a file cannot have says the archive was made to do harm
    private static void CheckName(ZipArchiveEntry entry)
    {
        string name = entry.FullName.Replace('\\', '/');
        if (Path.IsPathRooted(name) || name.Split('/').Contains("..") || name.Contains('\0', StringComparison.Ordinal))
        {
            throw new InvalidArchiveException(InvalidArchiveException.EntryNameUnsafe, $"The name '{entry.FullName}' in the archive is not allowed.", entry.FullName);
        }
    }

    private static void CheckExtension(ZipArchiveEntry entry, ArchiveKind kind, IntakeOptions limits)
    {
        string extension = Path.GetExtension(entry.Name);
        bool allowed = kind == ArchiveKind.Xml
            ? extension.Equals(".xml", StringComparison.OrdinalIgnoreCase)
            : limits.TabularExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        if (!allowed)
        {
            throw new InvalidArchiveException(
                InvalidArchiveException.EntryNotAllowed,
                $"The file '{entry.FullName}' is not one this endpoint takes ({(kind == ArchiveKind.Xml ? ".xml" : string.Join(", ", limits.TabularExtensions))}).",
                entry.FullName);
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
            throw new InvalidArchiveException(InvalidArchiveException.XmlNotWellFormed, $"The file '{entry.FullName}' is not well formed XML: {exception.Message}", entry.FullName);
        }
    }
}
