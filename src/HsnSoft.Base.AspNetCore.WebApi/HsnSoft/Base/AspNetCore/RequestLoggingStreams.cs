using System;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace HsnSoft.Base.AspNetCore;

/// <summary>Passes the request body on and counts what the application read of it (a chunked upload and a gRPC stream have no length).</summary>
internal sealed class CountingReadStream(Stream inner) : Stream
{
    public long Count { get; private set; }

    public override bool CanRead => inner.CanRead;

    public override bool CanSeek => inner.CanSeek;

    public override bool CanWrite => false;

    public override long Length => inner.Length;

    public override long Position
    {
        get => inner.Position;
        set => inner.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count) => Counted(inner.Read(buffer, offset, count));

    public override int Read(Span<byte> buffer) => Counted(inner.Read(buffer));

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => Counted(await inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false));

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => Counted(await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false));

    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override void Flush()
    {
    }

    private int Counted(int read)
    {
        Count += read;
        return read;
    }
}

/// <summary>
/// Sits in front of the response body: counts every byte on its way out (a download's size) and, when asked, keeps a copy of a
/// small text response so it can be logged. Nothing is buffered for the client: a file is streamed as it was, and a file sent with
/// <c>SendFileAsync</c> is still sent by the server (it is only measured).
/// </summary>
internal sealed class ResponseBodyTee(IHttpResponseBodyFeature inner, Func<bool> shouldCapture, int maxCapture) : Stream, IHttpResponseBodyFeature
{
    private MemoryStream? _capture;
    private PipeWriter? _writer;
    private bool _decided;
    private bool _capturing;

    /// <summary>The bytes that were written, headers excluded.</summary>
    public long Count { get; private set; }

    /// <summary>What was written as text, or <see langword="null"/> when it was not asked for or it is longer than the limit.</summary>
    public string? Body => _capturing && _capture is { Length: > 0 } ? Encoding.UTF8.GetString(_capture.GetBuffer(), 0, (int)_capture.Length) : null;

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    Stream IHttpResponseBodyFeature.Stream => this;

    PipeWriter IHttpResponseBodyFeature.Writer => _writer ??= PipeWriter.Create(this, new StreamPipeWriterOptions(leaveOpen: true));

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        Track(buffer);
        inner.Stream.Write(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        Track(buffer.Span);
        return inner.Stream.WriteAsync(buffer, cancellationToken);
    }

    public override void Flush() => inner.Stream.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) => inner.Stream.FlushAsync(cancellationToken);

    public Task StartAsync(CancellationToken cancellationToken = default) => inner.StartAsync(cancellationToken);

    public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
    {
        Count += count ?? new FileInfo(path).Length - offset;
        return inner.SendFileAsync(path, offset, count, cancellationToken);
    }

    public async Task CompleteAsync()
    {
        if (_writer is not null)
        {
            await _writer.CompleteAsync().ConfigureAwait(false);
        }

        await inner.CompleteAsync().ConfigureAwait(false);
    }

    public void DisableBuffering() => inner.DisableBuffering();

    /// <summary>
    /// Sends what was written through <see cref="IHttpResponseBodyFeature.Writer"/> and is still waiting in its buffer (gRPC writes a message there and ends the call without a flush of its own). It has to be
    /// done before the original body feature is put back: after that nothing would ever flush this writer, and the answer would go out without its body.
    /// </summary>
    public async ValueTask FlushWriterAsync()
    {
        if (_writer is not null)
        {
            await _writer.FlushAsync().ConfigureAwait(false);
        }
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    private void Track(ReadOnlySpan<byte> buffer)
    {
        Count += buffer.Length;
        if (!_decided)
        {
            // the first bytes are the first moment the content type is final
            _decided = true;
            _capturing = shouldCapture();
            _capture = _capturing ? new MemoryStream() : null;
        }

        if (_capturing)
        {
            if (_capture!.Length + buffer.Length > maxCapture)
            {
                // too long to be worth logging: the rest is only counted
                _capturing = false;
                _capture = null;
            }
            else
            {
                _capture.Write(buffer);
            }
        }
    }
}
