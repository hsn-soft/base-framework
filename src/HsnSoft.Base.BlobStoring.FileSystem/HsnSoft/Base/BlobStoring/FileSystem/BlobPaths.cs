using System;
using System.IO;
using System.Linq;

namespace HsnSoft.Base.BlobStoring.FileSystem;

/// <summary>
/// Where a blob is on the disk, and the proof that it is inside the root. A container is one folder name; a blob name is made of folder names and a file name with <c>/</c> between them. Every part has to be a plain name: no <c>.</c> or
/// <c>..</c>, no separator of the other system, no control character, no character a file system gives a meaning to (<c>:</c>, <c>*</c>, <c>?</c>), no name that ends with a dot or a space, no device name (<c>CON</c>, <c>NUL</c>, <c>COM1</c>)
/// and none that looks like the temporary file of a write. That is the same on every system, so a name that works on a laptop works on a server.
/// </summary>
public static class BlobPaths
{
    /// <summary>The start of the name of the temporary file a write goes to before it takes its place.</summary>
    public const string TemporaryPrefix = ".hsn-blob-";

    private const int MaxPartLength = 255;

    private static readonly char[] s_forbidden = [.. Path.GetInvalidFileNameChars(), '\\', ':', '*', '?', '"', '<', '>', '|'];

    private static readonly string[] s_devices = ["CON", "PRN", "AUX", "NUL", .. Enumerable.Range(1, 9).Select(number => $"COM{number}"), .. Enumerable.Range(1, 9).Select(number => $"LPT{number}")];

    /// <summary>The full path of a blob under <paramref name="root"/>; an <see cref="ArgumentException"/> names what is wrong with the container or the name.</summary>
    /// <param name="root">The absolute root, without a trailing separator.</param>
    /// <param name="container">The container (one folder).</param>
    /// <param name="name">The name of the blob (<c>invoices/2026/10/a.pdf</c>).</param>
    public static string Resolve(string root, string container, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        CheckPart(container, nameof(container));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        string[] parts = name.Split('/');
        foreach (string part in parts)
        {
            CheckPart(part, nameof(name));
        }

        string full = Path.GetFullPath(Path.Combine([root, container, .. parts]));
        string prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!full.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new ArgumentException("The name is outside of the root.", nameof(name));
        }

        return full;
    }

    /// <summary>The folder of a container under <paramref name="root"/>.</summary>
    public static string ResolveContainer(string root, string container)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        CheckPart(container, nameof(container));

        return Path.Combine(root, container);
    }

    /// <summary>Whether a part is a plain name (see the class).</summary>
    public static bool IsPlainName(string? part)
    {
        if (string.IsNullOrEmpty(part) || part.Length > MaxPartLength || part is "." or "..")
        {
            return false;
        }

        if (part.EndsWith('.') || part.EndsWith(' ') || part.StartsWith(' ') || part.StartsWith(TemporaryPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (part.Any(character => char.IsControl(character) || Array.IndexOf(s_forbidden, character) >= 0))
        {
            return false;
        }

        string withoutExtension = part.Contains('.', StringComparison.Ordinal) ? part[..part.IndexOf('.', StringComparison.Ordinal)] : part;
        return !s_devices.Contains(withoutExtension, StringComparer.OrdinalIgnoreCase);
    }

    private static void CheckPart(string? part, string parameter)
    {
        if (!IsPlainName(part))
        {
            throw new ArgumentException($"'{part}' is not a plain name: no '.' or '..', no separator, no control character, none of : * ? \" < > |, no trailing dot or space, no device name, at most {MaxPartLength} characters.", parameter);
        }
    }
}
