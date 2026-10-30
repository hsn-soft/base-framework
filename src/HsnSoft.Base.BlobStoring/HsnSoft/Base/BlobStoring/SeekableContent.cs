using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HsnSoft.Base.BlobStoring;

/// <summary>
/// S3 wants the size of an object before it is sent, and a stream that cannot seek (a request body) cannot tell it. This makes such a stream one that can, **without holding all of it in memory**: up to
/// <see cref="MemoryLimit"/> bytes stay in memory (the common small upload costs nothing more than it did), a larger one is written to a temporary file that is deleted when the stream is disposed.
/// </summary>
public static class SeekableContent
{
    /// <summary>How much of a stream that cannot seek is kept in memory; what is beyond goes to a temporary file.</summary>
    public const int MemoryLimit = 1024 * 1024;

    private const int BufferSize = 81920;

    /// <summary>
    /// The content itself when it can seek; otherwise a stream, positioned at its start, with a copy of it (in memory up to <see cref="MemoryLimit"/>, in a temporary file beyond). The caller disposes what it gets, which removes the temporary file.
    /// </summary>
    public static async Task<Stream> EnsureSeekableAsync(Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.CanSeek)
        {
            return content;
        }

        MemoryStream memory = new();
        byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            bool ended = false;
            while (memory.Length < MemoryLimit)
            {
                int read = await content.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    ended = true;
                    break;
                }

                memory.Write(buffer, 0, read);
            }

            if (ended)
            {
                memory.Position = 0;
                return memory;
            }

            FileStream file = new(
                Path.Combine(Path.GetTempPath(), $"hsnsoft-blob-{Guid.NewGuid():N}.tmp"),
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.DeleteOnClose);
            try
            {
                memory.Position = 0;
                await memory.CopyToAsync(file, BufferSize, cancellationToken).ConfigureAwait(false);
                await content.CopyToAsync(file, BufferSize, cancellationToken).ConfigureAwait(false);
                file.Position = 0;
                return file;
            }
            catch
            {
                await file.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
