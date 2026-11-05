using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.BlobStoring.FileSystem.Tests;

public sealed class FileSystemBlobStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"hsn-fs-{Guid.NewGuid():N}");

    public FileSystemBlobStoreTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    private FileSystemBlobStore Store(string? root = null) => new(Options.Create(new FileSystemBlobOptions { RootPath = root ?? Path.Combine(_root, "blobs") }));

    private static MemoryStream Bytes(string text) => new(Encoding.UTF8.GetBytes(text));

    private static async Task<string> ReadAllAsync(Stream? stream)
    {
        Assert.NotNull(stream);
        await using (stream)
        {
            using StreamReader reader = new(stream);
            return await reader.ReadToEndAsync();
        }
    }

    /// <summary>A stream that cannot seek and gives a part before it fails.</summary>
    private sealed class FailingStream(int goodBytes) : Stream
    {
        private int _given;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_given >= goodBytes)
            {
                throw new IOException("the connection broke");
            }

            int count = Math.Min(buffer.Length, goodBytes - _given);
            buffer.Span[..count].Fill(7);
            _given += count;
            return ValueTask.FromResult(count);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private static string[] Files(string folder) => Directory.GetFiles(folder, "*", SearchOption.AllDirectories);

    // ---------------------------------------------------------------- the store

    [Fact]
    public async Task ABlob_IsSaved_ReadBack_ReplacedAndDeleted()
    {
        FileSystemBlobStore store = Store();

        await store.SaveAsync("invoices", "2026/10/a.pdf", Bytes("first"), "application/pdf");
        Assert.Equal("first", await ReadAllAsync(await store.OpenReadAsync("invoices", "2026/10/a.pdf")));
        await store.SaveAsync("invoices", "2026/10/a.pdf", Bytes("second"), "application/pdf");
        Assert.Equal("second", await ReadAllAsync(await store.OpenReadAsync("invoices", "2026/10/a.pdf")));
        await store.DeleteAsync("invoices", "2026/10/a.pdf");

        Assert.Null(await store.OpenReadAsync("invoices", "2026/10/a.pdf"));
        Assert.Empty(Files(store.RootPath));
    }

    [Fact]
    public async Task ABlobThatIsNotThere_IsNull_AndDeletingItIsNotAnError()
    {
        FileSystemBlobStore store = Store();

        Assert.Null(await store.OpenReadAsync("nothing", "here.txt"));
        await store.DeleteAsync("nothing", "here.txt");
        await store.SaveAsync("c", "a.txt", Bytes("x"), "text/plain");
        Assert.Null(await store.OpenReadAsync("c", "other.txt"));
        await store.DeleteAsync("c", "other.txt");
        Assert.Single(Files(store.RootPath));
    }

    [Fact]
    public async Task AStreamThatCannotSeek_AndIsLarge_IsCopiedWithoutBeingHeld()
    {
        FileSystemBlobStore store = Store();
        byte[] big = new byte[5 * 1024 * 1024];
        new Random(1).NextBytes(big);
        await using NonSeekable source = new(big);

        await store.SaveAsync("c", "big.bin", source, "application/octet-stream");

        await using Stream read = (await store.OpenReadAsync("c", "big.bin"))!;
        using MemoryStream copy = new();
        await read.CopyToAsync(copy);
        Assert.Equal(big, copy.ToArray());
    }

    private sealed class NonSeekable(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;
    }

    [Fact]
    public async Task AWriteThatFails_LeavesNothingBehind_AndKeepsTheOldBlob()
    {
        FileSystemBlobStore store = Store();
        await store.SaveAsync("c", "a.txt", Bytes("old"), "text/plain");

        await Assert.ThrowsAsync<IOException>(() => store.SaveAsync("c", "a.txt", new FailingStream(200_000), "text/plain"));
        await Assert.ThrowsAsync<IOException>(() => store.SaveAsync("c", "new.txt", new FailingStream(10), "text/plain"));

        Assert.Equal("old", await ReadAllAsync(await store.OpenReadAsync("c", "a.txt")));
        Assert.Single(Files(store.RootPath));
    }

    [Fact]
    public async Task ACancelledWrite_LeavesNothingBehind()
    {
        FileSystemBlobStore store = Store();
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync("c", "a.txt", new MemoryStream(new byte[1_000_000]), "text/plain", cancelled.Token));

        Assert.Empty(Files(store.RootPath));
    }

    [Fact]
    public async Task ConcurrentWritesOfOneName_LeaveOneWholeBlob()
    {
        FileSystemBlobStore store = Store();
        byte[][] versions = [.. Enumerable.Range(0, 8).Select(number => Enumerable.Repeat((byte)number, 300_000).ToArray())];

        await Task.WhenAll(versions.Select(version => store.SaveAsync("c", "race.bin", new MemoryStream(version), "x")));

        await using Stream read = (await store.OpenReadAsync("c", "race.bin"))!;
        using MemoryStream copy = new();
        await read.CopyToAsync(copy);
        byte[] result = copy.ToArray();
        Assert.Equal(300_000, result.Length);
        Assert.All(result, value => Assert.Equal(result[0], value));
        Assert.Single(Files(store.RootPath));
    }

    [Fact]
    public async Task AFileThatIsBeingRead_CanBeReplaced()
    {
        FileSystemBlobStore store = Store();
        await store.SaveAsync("c", "a.txt", Bytes("old"), "text/plain");

        await using Stream reading = (await store.OpenReadAsync("c", "a.txt"))!;
        await store.SaveAsync("c", "a.txt", Bytes("new"), "text/plain");

        Assert.Equal("new", await ReadAllAsync(await store.OpenReadAsync("c", "a.txt")));
    }

    // ---------------------------------------------------------------- the names

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("a/../../outside.txt")]
    [InlineData("a/./b.txt")]
    [InlineData("/etc/passwd")]
    [InlineData("a//b.txt")]
    [InlineData("a\\b.txt")]
    [InlineData("C:\\windows\\x.txt")]
    [InlineData("a:stream")]
    [InlineData("nul")]
    [InlineData("com1.txt")]
    [InlineData("trailing.")]
    [InlineData("trailing ")]
    [InlineData(" leading")]
    [InlineData("what?.txt")]
    [InlineData("pipe|.txt")]
    [InlineData("tab\t.txt")]
    [InlineData("nul\0.txt")]
    [InlineData(".hsn-blob-abc.tmp")]
    [InlineData("")]
    [InlineData("  ")]
    public async Task ANameThatIsNotAPlainName_IsRefused_OnEveryCall(string name)
    {
        FileSystemBlobStore store = Store();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.SaveAsync("c", name, Bytes("x"), "text/plain"));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.OpenReadAsync("c", name));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.DeleteAsync("c", name));
        Assert.Empty(Files(_root));
    }

    [Theory]
    [InlineData("..")]
    [InlineData(".")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("/abs")]
    [InlineData("")]
    [InlineData("con")]
    public async Task AContainerThatIsNotOneFolderName_IsRefused(string container)
    {
        FileSystemBlobStore store = Store();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.SaveAsync(container, "a.txt", Bytes("x"), "text/plain"));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.OpenReadAsync(container, "a.txt"));
        Assert.Empty(Files(_root));
    }

    [Fact]
    public void APlainName_HasNoLimitOnItsKindOfLetters_ButOnItsLength()
    {
        Assert.True(BlobPaths.IsPlainName("çiğ köfte ürünü-2026_v1.final.pdf"));
        Assert.True(BlobPaths.IsPlainName(new string('a', 255)));
        Assert.False(BlobPaths.IsPlainName(new string('a', 256)));
        Assert.False(BlobPaths.IsPlainName(null));
        Assert.True(BlobPaths.IsPlainName("console.txt"));
        Assert.False(BlobPaths.IsPlainName("CON.txt"));
        Assert.False(BlobPaths.IsPlainName("lpt9"));
    }

    [Fact]
    public void TheResolvedPath_IsAlwaysUnderTheRoot()
    {
        string root = Path.Combine(_root, "blobs");

        Assert.Equal(Path.Combine(root, "c", "a", "b.txt"), BlobPaths.Resolve(root, "c", "a/b.txt"));
        Assert.Equal(Path.Combine(root, "c"), BlobPaths.ResolveContainer(root, "c"));
        Assert.Throws<ArgumentException>(() => BlobPaths.Resolve("", "c", "a"));
        Assert.Throws<ArgumentException>(() => BlobPaths.ResolveContainer(" ", "c"));
    }

    [Fact]
    public async Task AFolderThatIsALink_IsNeverWrittenThrough()
    {
        if (OperatingSystem.IsWindows())
        {
            return;   // a link needs a privilege there
        }

        FileSystemBlobStore store = Store();
        string outside = Path.Combine(_root, "outside");
        Directory.CreateDirectory(outside);
        Directory.CreateDirectory(Path.Combine(store.RootPath, "c"));
        Directory.CreateSymbolicLink(Path.Combine(store.RootPath, "c", "escape"), outside);
        await File.WriteAllTextAsync(Path.Combine(outside, "secret.txt"), "secret");

        await Assert.ThrowsAsync<IOException>(() => store.SaveAsync("c", "escape/x.txt", Bytes("x"), "text/plain"));
        await Assert.ThrowsAsync<IOException>(() => store.OpenReadAsync("c", "escape/secret.txt"));
        await Assert.ThrowsAsync<IOException>(() => store.DeleteAsync("c", "escape/secret.txt"));

        Assert.True(File.Exists(Path.Combine(outside, "secret.txt")));
        Assert.Single(Files(outside));
    }

    // ---------------------------------------------------------------- the root and the registration

    [Fact]
    public void TheRoot_IsRequired_AbsoluteAndCreated()
    {
        Assert.Throws<InvalidOperationException>(() => Store(""));
        Assert.Throws<InvalidOperationException>(() => Store("relative/blobs"));
        Assert.Throws<ArgumentNullException>(() => new FileSystemBlobStore(null!));

        string made = Path.Combine(_root, "made", "deep");
        FileSystemBlobStore store = Store(made + Path.DirectorySeparatorChar);

        Assert.True(Directory.Exists(made));
        Assert.Equal(made, store.RootPath);
    }

    [Fact]
    public async Task TheRegistration_GivesTheStoreAndAReadinessCheck()
    {
        string root = Path.Combine(_root, "registered");
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["FileSystem:RootPath"] = root }).Build();
        ServiceCollection services = new();
        services.AddLogging().AddFileSystemBlobStoring(configuration);
        await using ServiceProvider provider = services.BuildServiceProvider();

        IBlobStore store = provider.GetRequiredService<IBlobStore>();
        await store.SaveAsync("c", "a.txt", Bytes("x"), "text/plain");
        HealthCheckService health = provider.GetRequiredService<HealthCheckService>();
        HealthReport report = await health.CheckHealthAsync(entry => entry.Name == "blob-filesystem");

        Assert.Same(store, provider.GetRequiredService<FileSystemBlobStore>());
        Assert.Equal(HealthStatus.Healthy, report.Status);
        Assert.Empty(Directory.GetFiles(root, ".hsn-blob-*"));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddFileSystemBlobStoring(configuration));
        Assert.Throws<ArgumentNullException>(() => services.AddFileSystemBlobStoring(null!));
    }

    [Fact]
    public async Task TheReadinessCheck_IsUnhealthy_WhenTheRootCannotBeWritten()
    {
        string root = Path.Combine(_root, "gone");
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["FileSystem:RootPath"] = root }).Build();
        ServiceCollection services = new();
        services.AddLogging().AddFileSystemBlobStoring(configuration);
        await using ServiceProvider provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IBlobStore>();
        Directory.Delete(root, true);

        HealthReport report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync(entry => entry.Name == "blob-filesystem");

        Assert.Equal(HealthStatus.Unhealthy, report.Status);
    }
}
