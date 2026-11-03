using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.BlobStoring.FileSystem;

/// <summary>
/// <see cref="IBlobStore"/> on the local disk: a container is a folder under the root and a blob a file in it (a name with <c>/</c> is folders). A write goes to a temporary file in the folder of the blob and takes the place of the blob
/// only when it is complete (an atomic move), so a reader sees the old blob or the whole new one, and a write that fails or is cancelled leaves nothing behind. The content is copied as it arrives, never held in memory. A name cannot leave the
/// root (<see cref="BlobPaths"/>) and neither can a link: a folder under the root that is a link is refused. The content type is not kept (a disk has no place for it); a service that needs it stores it itself.
/// </summary>
public sealed class FileSystemBlobStore : IBlobStore
{
    private const int BufferSize = 81920;

    public FileSystemBlobStore(IOptions<FileSystemBlobOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string configured = options.Value.RootPath;
        if (string.IsNullOrWhiteSpace(configured) || !Path.IsPathRooted(configured))
        {
            throw new InvalidOperationException("FileSystem:RootPath is required and has to be an absolute path.");
        }

        RootPath = Path.GetFullPath(configured).TrimEnd(Path.DirectorySeparatorChar);
        Directory.CreateDirectory(RootPath);
    }

    /// <summary>The root every container is under.</summary>
    public string RootPath { get; }

    public async Task SaveAsync(string container, string name, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        string target = BlobPaths.Resolve(RootPath, container, name);
        string folder = Path.GetDirectoryName(target)!;
        Directory.CreateDirectory(folder);
        EnsureNoLinks(folder);

        string temporary = Path.Combine(folder, $"{BlobPaths.TemporaryPrefix}{Guid.NewGuid():N}.tmp");
        try
        {
            FileStream file = new(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous);
            await using (file.ConfigureAwait(false))
            {
                await content.CopyToAsync(file, BufferSize, cancellationToken).ConfigureAwait(false);
                await file.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporary, target, overwrite: true);
        }
        catch
        {
            TryDelete(temporary);
            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(string container, string name, CancellationToken cancellationToken = default)
    {
        string path = BlobPaths.Resolve(RootPath, container, name);
        string? folder = Path.GetDirectoryName(path);
        if (folder is null || !File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        EnsureNoLinks(folder);
        try
        {
            return Task.FromResult<Stream?>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan));
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            // deleted between the check and the open
            return Task.FromResult<Stream?>(null);
        }
    }

    public Task DeleteAsync(string container, string name, CancellationToken cancellationToken = default)
    {
        string path = BlobPaths.Resolve(RootPath, container, name);
        if (Path.GetDirectoryName(path) is { } folder && Directory.Exists(folder))
        {
            EnsureNoLinks(folder);
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    // a folder under the root that is a link could take a write outside of it: every folder from the root down has to be a real one
    private void EnsureNoLinks(string folder)
    {
        string current = folder;
        while (current.Length > RootPath.Length)
        {
            if (Directory.Exists(current) && new DirectoryInfo(current).LinkTarget is not null)
            {
                throw new IOException($"'{Path.GetFileName(current)}' is a link: a blob is never stored through one.");
            }

            current = Path.GetDirectoryName(current)!;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // what could not be removed is a temporary file with a name of its own; the next write does not touch it
        }
        catch (UnauthorizedAccessException)
        {
            // the same
        }
    }
}
