using System.Text;
using HsnSoft.Base.BlobStoring;
using HsnSoft.Base.BlobStoring.Minio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Testcontainers.Minio;

namespace HsnSoft.Base.IntegrationTests.BlobStoring;

public sealed class MinioTests : IAsyncLifetime
{
    private readonly MinioContainer _container = new MinioBuilder("minio/minio:RELEASE.2024-08-29T01-40-52Z").Build();
    private IHost _host = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var configuration = Support.Configuration(
            ("Minio:Endpoint", $"{_container.Hostname}:{_container.GetMappedPublicPort(9000)}"),
            ("Minio:AccessKey", _container.GetAccessKey()),
            ("Minio:SecretKey", _container.GetSecretKey()));
        _host = await Support.StartHostAsync(services => services.AddMinioBlobStoring(configuration));
    }

    public async Task DisposeAsync()
    {
        _host.Dispose();
        await _container.DisposeAsync();
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

    [Fact]
    public async Task SavedBlob_CanBeReadBack_AndTheBucketIsCreatedOnTheFly()
    {
        var bucket = NewBucket();

        await Store.SaveAsync(bucket, "a/one.json", new MemoryStream(Encoding.UTF8.GetBytes("{\"n\":1}")), "application/json");
        await Store.SaveAsync(bucket, "a/two.json", new MemoryStream(Encoding.UTF8.GetBytes("{\"n\":2}")), "application/json");

        Assert.Equal("{\"n\":1}", await ReadAllAsync(await Store.OpenReadAsync(bucket, "a/one.json")));
        Assert.Equal("{\"n\":2}", await ReadAllAsync(await Store.OpenReadAsync(bucket, "a/two.json")));
    }

    [Fact]
    public async Task SavingTheSameNameAgain_ReplacesTheBlob()
    {
        var bucket = NewBucket();

        await Store.SaveAsync(bucket, "x.txt", new MemoryStream("old"u8.ToArray()), "text/plain");
        await Store.SaveAsync(bucket, "x.txt", new MemoryStream("new"u8.ToArray()), "text/plain");

        Assert.Equal("new", await ReadAllAsync(await Store.OpenReadAsync(bucket, "x.txt")));
    }

    [Fact]
    public async Task StreamThatCannotSeek_IsBufferedAndSaved()
    {
        var bucket = NewBucket();

        await Store.SaveAsync(bucket, "body.txt", new ForwardOnlyStream("streamed"u8.ToArray()), "text/plain");

        Assert.Equal("streamed", await ReadAllAsync(await Store.OpenReadAsync(bucket, "body.txt")));
    }

    [Fact]
    public async Task MissingBlobOrBucket_ReadsAsNull()
    {
        var bucket = NewBucket();
        await Store.SaveAsync(bucket, "exists.txt", new MemoryStream("x"u8.ToArray()), "text/plain");

        Assert.Null(await Store.OpenReadAsync(bucket, "missing.txt"));
        Assert.Null(await Store.OpenReadAsync(NewBucket(), "anything.txt"));
    }

    [Fact]
    public async Task DeletedBlob_IsGone_AndDeletingTwiceOrFromAMissingBucketIsFine()
    {
        var bucket = NewBucket();
        await Store.SaveAsync(bucket, "gone.txt", new MemoryStream("x"u8.ToArray()), "text/plain");

        await Store.DeleteAsync(bucket, "gone.txt");
        await Store.DeleteAsync(bucket, "gone.txt");
        await Store.DeleteAsync(NewBucket(), "never-existed.txt");

        Assert.Null(await Store.OpenReadAsync(bucket, "gone.txt"));
    }

    [Fact]
    public async Task ServiceIsReady_WhenMinioIsUp()
    {
        Assert.Equal(HealthStatus.Healthy, (await Support.ReadyReportAsync(_host.Services)).Status);
    }
}
