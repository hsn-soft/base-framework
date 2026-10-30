using System.Security.Cryptography;

namespace HsnSoft.Base.BlobStoring.Tests;

public class SeekableContentTests
{
    /// <summary>A stream that cannot seek or tell its length, like a request body; its bytes are made as they are read.</summary>
    private sealed class ForwardOnlyStream(long length, long failAfter = -1) : Stream
    {
        private long _position;

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

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (failAfter >= 0 && _position >= failAfter)
            {
                throw new IOException("the connection of the request broke");
            }

            int toRead = (int)Math.Min(count, length - _position);
            for (int index = 0; index < toRead; index++)
            {
                buffer[offset + index] = (byte)((_position + index) % 251);
            }

            _position += toRead;
            return toRead;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private static byte[] Expected(long length)
    {
        byte[] bytes = new byte[length];
        for (long index = 0; index < length; index++)
        {
            bytes[index] = (byte)(index % 251);
        }

        return bytes;
    }

    private static string[] TemporaryFiles() => Directory.GetFiles(Path.GetTempPath(), "hsnsoft-blob-*.tmp");

    private static async Task<byte[]> ReadAllAsync(Stream stream)
    {
        using MemoryStream copy = new();
        await stream.CopyToAsync(copy);
        return copy.ToArray();
    }

    [Fact]
    public async Task AStreamThatCanSeek_IsReturnedAsItIs()
    {
        using MemoryStream content = new([1, 2, 3]);

        Stream result = await SeekableContent.EnsureSeekableAsync(content);

        Assert.Same(content, result);
    }

    [Fact]
    public async Task ASmallStreamThatCannotSeek_IsKeptInMemory_FromItsStart()
    {
        string[] before = TemporaryFiles();

        await using Stream result = await SeekableContent.EnsureSeekableAsync(new ForwardOnlyStream(1000));

        Assert.IsType<MemoryStream>(result);
        Assert.True(result.CanSeek);
        Assert.Equal((0L, 1000L), (result.Position, result.Length));
        Assert.Equal(Expected(1000), await ReadAllAsync(result));
        Assert.Equal(before, TemporaryFiles());
    }

    [Fact]
    public async Task AnEmptyStream_IsAnEmptyOneThatCanSeek()
    {
        await using Stream result = await SeekableContent.EnsureSeekableAsync(new ForwardOnlyStream(0));

        Assert.True(result.CanSeek);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public async Task ALargeStream_GoesToATemporaryFile_WholeAndFromItsStart_AndTheFileIsRemovedWithTheStream()
    {
        const long size = (3 * SeekableContent.MemoryLimit) + 12345;
        string[] before = TemporaryFiles();

        Stream result = await SeekableContent.EnsureSeekableAsync(new ForwardOnlyStream(size));

        FileStream file = Assert.IsType<FileStream>(result);
        Assert.Equal((0L, size), (file.Position, file.Length));
        Assert.Equal(before.Length + 1, TemporaryFiles().Length);
        Assert.Equal(SHA256.HashData(Expected(size)), await SHA256.HashDataAsync(file));

        await file.DisposeAsync();

        Assert.Equal(before, TemporaryFiles());
    }

    [Fact]
    public async Task AStreamThatBreaksAfterTheMemoryLimit_LeavesNoFileBehind()
    {
        string[] before = TemporaryFiles();

        await Assert.ThrowsAsync<IOException>(() => SeekableContent.EnsureSeekableAsync(new ForwardOnlyStream(5 * SeekableContent.MemoryLimit, failAfter: 2 * SeekableContent.MemoryLimit)));

        Assert.Equal(before, TemporaryFiles());
    }

    [Fact]
    public async Task ACancellation_StopsTheCopy()
    {
        using CancellationTokenSource cancel = new();
        await cancel.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SeekableContent.EnsureSeekableAsync(new ForwardOnlyStream(1000), cancel.Token));
    }

    [Fact]
    public async Task NullContent_IsRejected()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => SeekableContent.EnsureSeekableAsync(null!));
    }
}
