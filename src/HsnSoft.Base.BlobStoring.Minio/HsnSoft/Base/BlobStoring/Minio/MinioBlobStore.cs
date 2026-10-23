using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace HsnSoft.Base.BlobStoring.Minio;

public sealed class MinioBlobStore : IBlobStore
{
    private readonly IMinioClient _client;
    private readonly ConcurrentDictionary<string, bool> _knownBuckets = new();

    public MinioBlobStore(IMinioClient client) => _client = client;

    public async Task SaveAsync(string container, string name, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        await EnsureBucketAsync(container, cancellationToken).ConfigureAwait(false);

        // S3 needs the size up front; buffer when the stream cannot tell it
        var stream = content;
        if (!content.CanSeek)
        {
            stream = new MemoryStream();
            await content.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
            stream.Position = 0;
        }

        await using (stream.ConfigureAwait(false))
        {
            var arguments = new PutObjectArgs()
                .WithBucket(container)
                .WithObject(name)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length - stream.Position)
                .WithContentType(contentType);
            await _client.PutObjectAsync(arguments, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<Stream?> OpenReadAsync(string container, string name, CancellationToken cancellationToken = default)
    {
        var buffer = new MemoryStream();
        try
        {
            var arguments = new GetObjectArgs()
                .WithBucket(container)
                .WithObject(name)
                .WithCallbackStream((source, token) => source.CopyToAsync(buffer, token));
            await _client.GetObjectAsync(arguments, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is ObjectNotFoundException or BucketNotFoundException)
        {
            await buffer.DisposeAsync().ConfigureAwait(false);
            return null;
        }

        buffer.Position = 0;
        return buffer;
    }

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
