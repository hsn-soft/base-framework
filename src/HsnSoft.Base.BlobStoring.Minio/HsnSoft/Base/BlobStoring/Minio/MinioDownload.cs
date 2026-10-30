using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace HsnSoft.Base.BlobStoring.Minio;

/// <summary>
/// A download of an object that is read while it arrives. The client of MinIO hands the body to a callback and not to the caller, and the callback used to copy all of it into a <see cref="MemoryStream"/> (a few reads of a large object at the same time ran a pod
/// out of memory). Here the callback writes into a pipe whose writer waits when <see cref="PauseAt"/> bytes are not read yet, and the caller reads the other end: only a few megabytes of the object are in memory, whatever its size.
/// </summary>
internal sealed class MinioDownload : Stream
{
    internal const int PauseAt = 4 * 1024 * 1024;
    internal const int ResumeAt = 2 * 1024 * 1024;

    private readonly CancellationTokenSource _stop;
    private readonly Pipe _pipe;
    private readonly Stream _body;
    private readonly Task _download;
    private int _disposed;

    private MinioDownload(Pipe pipe, CancellationTokenSource stop, Task download)
    {
        _pipe = pipe;
        _stop = stop;
        _download = download;
        _body = pipe.Reader.AsStream();
    }

    /// <summary>Starts the download and waits for its first bytes (or its end): the object not being there is known then, as <see langword="null"/>, and not by the first read of a stream that was returned.</summary>
    internal static async Task<Stream?> OpenAsync(IMinioClient client, string container, string name, CancellationToken cancellationToken)
    {
        Pipe pipe = new(new PipeOptions(pauseWriterThreshold: PauseAt, resumeWriterThreshold: ResumeAt, useSynchronizationContext: false));
        CancellationTokenSource stop = new();
        Task download = Task.Run(() => CopyAsync(client, container, name, pipe.Writer, stop.Token), CancellationToken.None);

        // while this call waits, the caller's token ends the download; once the stream is returned, the stream owns it (disposing it ends the download)
        await using CancellationTokenRegistration registration = cancellationToken.Register(stop.Cancel);
        try
        {
            ReadResult first = await pipe.Reader.ReadAsync(stop.Token).ConfigureAwait(false);
            pipe.Reader.AdvanceTo(first.Buffer.Start, first.Buffer.Start);
        }
        catch (Exception exception) when (exception is ObjectNotFoundException or BucketNotFoundException)
        {
            await EndAsync(pipe, stop, download).ConfigureAwait(false);
            return null;
        }
        catch
        {
            await EndAsync(pipe, stop, download).ConfigureAwait(false);
            throw;
        }

        return new MinioDownload(pipe, stop, download);
    }

    private static async Task CopyAsync(IMinioClient client, string container, string name, PipeWriter writer, CancellationToken cancellationToken)
    {
        try
        {
            Stream sink = writer.AsStream(leaveOpen: true);
            GetObjectArgs arguments = new GetObjectArgs()
                .WithBucket(container)
                .WithObject(name)
                .WithCallbackStream((source, token) => source.CopyToAsync(sink, token));
            await client.GetObjectAsync(arguments, cancellationToken).ConfigureAwait(false);
            await writer.CompleteAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // the reader sees it: as the failure of its next read (or, before the first bytes, as the result of opening)
            await writer.CompleteAsync(exception).ConfigureAwait(false);
        }
    }

    private static async Task EndAsync(Pipe pipe, CancellationTokenSource stop, Task download)
    {
        await stop.CancelAsync().ConfigureAwait(false);
        await pipe.Reader.CompleteAsync().ConfigureAwait(false);
        await download.ConfigureAwait(false);
        stop.Dispose();
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) => _body.Read(buffer, offset, count);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _body.ReadAsync(buffer, cancellationToken);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _body.ReadAsync(buffer, offset, count, cancellationToken);

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            await EndAsync(_pipe, _stop, _download).ConfigureAwait(false);
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            // a stream disposed without await ends the download without waiting for it (nothing is left running: it was cancelled and its reader is gone)
            _ = EndAsync(_pipe, _stop, _download);
        }

        base.Dispose(disposing);
    }
}
