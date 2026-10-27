using Grpc.Core;

namespace Edx.IntakeService.Http.Host.GrpcApi;

/// <summary>The bytes of an upload that came as the messages of a gRPC stream, read as one stream (the first message, which names the file, has been read already).</summary>
internal sealed class ChunkedReadStream(IAsyncStreamReader<UploadChunk> source) : Stream
{
    private ReadOnlyMemory<byte> _current = ReadOnlyMemory<byte>.Empty;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        while (_current.IsEmpty)
        {
            if (!await source.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                return 0;
            }

            UploadChunk chunk = source.Current;
            if (chunk.PayloadCase != UploadChunk.PayloadOneofCase.Data)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "After the first message, which names the file, every message carries data."));
            }

            _current = chunk.Data.Memory;
        }

        int count = Math.Min(buffer.Length, _current.Length);
        _current[..count].CopyTo(buffer);
        _current = _current[count..];
        return count;
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("Read it asynchronously.");

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
