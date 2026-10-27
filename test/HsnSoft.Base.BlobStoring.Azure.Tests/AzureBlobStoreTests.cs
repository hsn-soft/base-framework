using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace HsnSoft.Base.BlobStoring.Azure.Tests;

public class AzureBlobStoreTests
{
    private sealed class FakeService(FakeContainer container) : BlobServiceClient
    {
        public override BlobContainerClient GetBlobContainerClient(string blobContainerName) => container;
    }

    private sealed class FakeContainer(FakeBlob blob) : BlobContainerClient
    {
        public int Creations { get; private set; }

        public int FailCreations { get; set; }

        public override Task<Response<BlobContainerInfo>> CreateIfNotExistsAsync(PublicAccessType publicAccessType = PublicAccessType.None, IDictionary<string, string>? metadata = null, BlobContainerEncryptionScopeOptions? encryptionScopeOptions = null, CancellationToken cancellationToken = default)
        {
            Creations++;
            if (FailCreations > 0)
            {
                FailCreations--;
                return Task.FromException<Response<BlobContainerInfo>>(new RequestFailedException(503, "unavailable"));
            }

            return Task.FromResult<Response<BlobContainerInfo>>(null!);
        }

        public override BlobClient GetBlobClient(string blobName) => blob;
    }

    private sealed class FakeBlob : BlobClient
    {
        public int Uploads { get; private set; }

        public string? ContentType { get; private set; }

        public override Task<Response<BlobContentInfo>> UploadAsync(Stream content, BlobUploadOptions options, CancellationToken cancellationToken = default)
        {
            Uploads++;
            ContentType = options.HttpHeaders.ContentType;
            return Task.FromResult<Response<BlobContentInfo>>(null!);
        }

        public override Task<Response<bool>> DeleteIfExistsAsync(DeleteSnapshotsOption snapshotsOption = DeleteSnapshotsOption.None, BlobRequestConditions? conditions = null, CancellationToken cancellationToken = default)
            => Task.FromException<Response<bool>>(new RequestFailedException(404, "no such container"));

        public override Task<Response<BlobDownloadStreamingResult>> DownloadStreamingAsync(BlobDownloadOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromException<Response<BlobDownloadStreamingResult>>(new RequestFailedException(404, "no such blob"));
    }

    [Fact]
    public async Task AContainerThatCouldNotBeMade_IsMadeAgainByTheNextSave_AndTheBlobIsSavedWithItsContentType()
    {
        FakeBlob blob = new();
        FakeContainer container = new(blob) { FailCreations = 1 };
        AzureBlobStore store = new(new FakeService(container));

        await Assert.ThrowsAsync<RequestFailedException>(() => store.SaveAsync("c", "a.txt", new MemoryStream("x"u8.ToArray()), "text/plain"));
        await store.SaveAsync("c", "a.txt", new MemoryStream("x"u8.ToArray()), "text/plain");
        await store.SaveAsync("c", "b.txt", new MemoryStream("y"u8.ToArray()), "text/plain");

        Assert.Equal(2, container.Creations);
        Assert.Equal(2, blob.Uploads);
        Assert.Equal("text/plain", blob.ContentType);
    }

    [Fact]
    public async Task AMissingContainerOrBlob_ReadsAsNull_AndDeletingFromItIsFine()
    {
        AzureBlobStore store = new(new FakeService(new FakeContainer(new FakeBlob())));

        Assert.Null(await store.OpenReadAsync("none", "n"));
        await store.DeleteAsync("none", "n");
    }
}
