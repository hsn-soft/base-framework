using System.Security.Cryptography;
using System.Text;
using HsnSoft.Base.BlobStoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HsnSoft.Base.IntegrationTests.BlobStoring;

/// <summary>What every <see cref="IBlobStore"/> does, whatever the store behind it: a provider derives from it, starts its store and gives a host that has it registered.</summary>
public abstract class BlobStoreContractTests : IAsyncLifetime
{
    private IHost _host = null!;

    /// <summary>Starts the store and returns a host with <see cref="IBlobStore"/> and its readiness check registered.</summary>
    protected abstract Task<IHost> StartAsync();

    protected abstract Task StopAsync();

    public async Task InitializeAsync() => _host = await StartAsync();

    public async Task DisposeAsync()
    {
        _host.Dispose();
        await StopAsync();
    }

    private IBlobStore Store => _host.Services.GetRequiredService<IBlobStore>();

    private static async Task<string> ReadAllAsync(Stream? stream)
    {
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private static string NewBucket() => $"it-{Guid.NewGuid():N}";

    /// <summary>A stream that cannot seek or tell its length, like a request body.</summary>
    private sealed class ForwardOnlyStream(byte[] bytes) : Stream
    {
        private readonly MemoryStream _inner = new(bytes);

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

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>A stream that cannot seek and whose bytes are made as they are read: a large body that is not held anywhere.</summary>
    private sealed class GeneratedStream(long length) : Stream
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

    private static byte[] ExpectedHash(long length)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] chunk = new byte[1024 * 1024];
        for (long position = 0; position < length; position += chunk.Length)
        {
            int count = (int)Math.Min(chunk.Length, length - position);
            for (int index = 0; index < count; index++)
            {
                chunk[index] = (byte)((position + index) % 251);
            }

            hash.AppendData(chunk, 0, count);
        }

        return hash.GetHashAndReset();
    }

    [Fact]
    public async Task ALargeStreamThatCannotSeek_IsSavedAndReadBack_AndLeavesNoTemporaryFile()
    {
        const long size = 48L * 1024 * 1024;
        string bucket = NewBucket();
        string[] before = Directory.GetFiles(Path.GetTempPath(), "hsnsoft-blob-*.tmp");

        await Store.SaveAsync(bucket, "large.bin", new GeneratedStream(size), "application/octet-stream");

        await using Stream stream = (await Store.OpenReadAsync(bucket, "large.bin"))!;
        Assert.Equal(ExpectedHash(size), await SHA256.HashDataAsync(stream));
        Assert.Equal(before, Directory.GetFiles(Path.GetTempPath(), "hsnsoft-blob-*.tmp"));
    }

    [Fact]
    public async Task ALargeBlob_IsReadWhileItArrives_NotBufferedInMemory()
    {
        const long size = 96L * 1024 * 1024;
        string bucket = NewBucket();
        await Store.SaveAsync(bucket, "large.bin", new GeneratedStream(size), "application/octet-stream");

        // opening it and reading a little: a store that read the whole object first would allocate its 96 MB (and the doubling of a buffer that grows) before the first byte
        long allocatedBefore = GC.GetTotalAllocatedBytes(precise: false);
        await using (Stream stream = (await Store.OpenReadAsync(bucket, "large.bin"))!)
        {
            await stream.ReadExactlyAsync(new byte[1024]);
        }

        long allocated = GC.GetTotalAllocatedBytes(precise: false) - allocatedBefore;
        Assert.True(allocated < 32L * 1024 * 1024, $"opening and reading 1 KB of a 96 MB blob allocated {allocated / (1024 * 1024)} MB");
    }

    [Fact]
    public async Task SavedBlob_CanBeReadBack_AndTheBucketIsCreatedOnTheFly()
    {
        string bucket = NewBucket();

        await Store.SaveAsync(bucket, "a/one.json", new MemoryStream(Encoding.UTF8.GetBytes("{\"n\":1}")), "application/json");
        await Store.SaveAsync(bucket, "a/two.json", new MemoryStream(Encoding.UTF8.GetBytes("{\"n\":2}")), "application/json");

        Assert.Equal("{\"n\":1}", await ReadAllAsync(await Store.OpenReadAsync(bucket, "a/one.json")));
        Assert.Equal("{\"n\":2}", await ReadAllAsync(await Store.OpenReadAsync(bucket, "a/two.json")));
    }

    [Fact]
    public async Task SavingTheSameNameAgain_ReplacesTheBlob()
    {
        string bucket = NewBucket();

        await Store.SaveAsync(bucket, "x.txt", new MemoryStream("old"u8.ToArray()), "text/plain");
        await Store.SaveAsync(bucket, "x.txt", new MemoryStream("new"u8.ToArray()), "text/plain");

        Assert.Equal("new", await ReadAllAsync(await Store.OpenReadAsync(bucket, "x.txt")));
    }

    [Fact]
    public async Task StreamThatCannotSeek_IsBufferedAndSaved()
    {
        string bucket = NewBucket();

        await Store.SaveAsync(bucket, "body.txt", new ForwardOnlyStream("streamed"u8.ToArray()), "text/plain");

        Assert.Equal("streamed", await ReadAllAsync(await Store.OpenReadAsync(bucket, "body.txt")));
    }

    [Fact]
    public async Task MissingBlobOrBucket_ReadsAsNull()
    {
        string bucket = NewBucket();
        await Store.SaveAsync(bucket, "exists.txt", new MemoryStream("x"u8.ToArray()), "text/plain");

        Assert.Null(await Store.OpenReadAsync(bucket, "missing.txt"));
        Assert.Null(await Store.OpenReadAsync(NewBucket(), "anything.txt"));
    }

    [Fact]
    public async Task DeletedBlob_IsGone_AndDeletingTwiceOrFromAMissingBucketIsFine()
    {
        string bucket = NewBucket();
        await Store.SaveAsync(bucket, "gone.txt", new MemoryStream("x"u8.ToArray()), "text/plain");

        await Store.DeleteAsync(bucket, "gone.txt");
        await Store.DeleteAsync(bucket, "gone.txt");
        await Store.DeleteAsync(NewBucket(), "never-existed.txt");

        Assert.Null(await Store.OpenReadAsync(bucket, "gone.txt"));
    }

    [Fact]
    public async Task ServiceIsReady_WhenTheStoreIsUp()
    {
        Assert.Equal(HealthStatus.Healthy, (await Support.ReadyReportAsync(_host.Services)).Status);
    }
}
