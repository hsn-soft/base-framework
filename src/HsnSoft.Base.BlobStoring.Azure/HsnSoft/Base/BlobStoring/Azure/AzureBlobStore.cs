using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace HsnSoft.Base.BlobStoring.Azure;

/// <summary><see cref="IBlobStore"/> on Azure Blob Storage: a container of the store is a container of the account, and it is made when the first blob is saved.</summary>
public sealed class AzureBlobStore(BlobServiceClient client) : IBlobStore
{
    private readonly BlobServiceClient _client = client;
    // the task that makes the container, so saves at the same time wait for it instead of reaching a container that is not there yet
    private readonly ConcurrentDictionary<string, Task> _containers = new();

    public async Task SaveAsync(string container, string name, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        BlobContainerClient containerClient = _client.GetBlobContainerClient(container);
        Task made = _containers.GetOrAdd(container, _ => containerClient.CreateIfNotExistsAsync(cancellationToken: CancellationToken.None));
        try
        {
            await made.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) when (made.IsFaulted)
        {
            // not made: the next save tries again
            _containers.TryRemove(new System.Collections.Generic.KeyValuePair<string, Task>(container, made));
            throw;
        }

        await containerClient.GetBlobClient(name)
            .UploadAsync(content, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } }, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Stream?> OpenReadAsync(string container, string name, CancellationToken cancellationToken = default)
    {
        try
        {
            Response<BlobDownloadStreamingResult> result = await _client.GetBlobContainerClient(container).GetBlobClient(name).DownloadStreamingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return result.Value.Content;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string container, string name, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.GetBlobContainerClient(container).GetBlobClient(name).DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // the container is not there: nothing to delete
        }
    }
}
