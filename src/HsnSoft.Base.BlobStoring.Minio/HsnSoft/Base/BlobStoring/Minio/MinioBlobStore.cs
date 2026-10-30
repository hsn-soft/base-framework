using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace HsnSoft.Base.BlobStoring.Minio;

public sealed class MinioBlobStore(IMinioClient client) : IBlobStore
{
    private readonly IMinioClient _client = client;
    private readonly ConcurrentDictionary<string, bool> _knownBuckets = new();

    public async Task SaveAsync(string container, string name, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        await EnsureBucketAsync(container, cancellationToken).ConfigureAwait(false);

        // S3 needs the size up front: a stream that cannot tell it is copied first, in memory up to a megabyte and in a temporary file beyond
        Stream stream = await SeekableContent.EnsureSeekableAsync(content, cancellationToken).ConfigureAwait(false);

        await using (stream.ConfigureAwait(false))
        {
            PutObjectArgs arguments = new PutObjectArgs()
                .WithBucket(container)
                .WithObject(name)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length - stream.Position)
                .WithContentType(contentType);
            await _client.PutObjectAsync(arguments, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task<Stream?> OpenReadAsync(string container, string name, CancellationToken cancellationToken = default)
        => MinioDownload.OpenAsync(_client, container, name, cancellationToken);

    public async Task DeleteAsync(string container, string name, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(container).WithObject(name), cancellationToken).ConfigureAwait(false);
        }
        catch (BucketNotFoundException)
        {
            // nothing to delete
        }
    }

    private async Task EnsureBucketAsync(string bucket, CancellationToken cancellationToken)
    {
        if (_knownBuckets.ContainsKey(bucket))
        {
            return;
        }

        if (!await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), cancellationToken).ConfigureAwait(false))
        {
            try
            {
                await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), cancellationToken).ConfigureAwait(false);
            }
            catch (MinioException)
            {
                // another instance created it between the check and now; anything else surfaces on the write
            }
        }

        _knownBuckets[bucket] = true;
    }
}
