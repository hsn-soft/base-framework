using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;

namespace HsnSoft.Base.BlobStoring.Aws;

/// <summary><see cref="IBlobStore"/> on Amazon S3 (or any store with its API): a container of the store is a bucket, made when the first blob is saved.</summary>
public sealed class AwsBlobStore(IAmazonS3 client) : IBlobStore
{
    private readonly IAmazonS3 _client = client;
    private readonly ConcurrentDictionary<string, bool> _knownBuckets = new();

    public async Task SaveAsync(string container, string name, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        await EnsureBucketAsync(container, cancellationToken).ConfigureAwait(false);

        // S3 needs the size up front; buffer when the stream cannot tell it
        Stream stream = content;
        if (!content.CanSeek)
        {
            stream = new MemoryStream();
            await content.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
            stream.Position = 0;
        }

        await using (stream.ConfigureAwait(false))
        {
            await _client.PutObjectAsync(new PutObjectRequest { BucketName = container, Key = name, InputStream = stream, ContentType = contentType, AutoCloseStream = false }, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<Stream?> OpenReadAsync(string container, string name, CancellationToken cancellationToken = default)
    {
        try
        {
            GetObjectResponse response = await _client.GetObjectAsync(container, name, cancellationToken).ConfigureAwait(false);
            return new ResponseStream(response);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string container, string name, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.DeleteObjectAsync(container, name, cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            // the bucket is not there: nothing to delete (a key that is not there is not an error for S3)
        }
    }

    private async Task EnsureBucketAsync(string bucket, CancellationToken cancellationToken)
    {
        if (_knownBuckets.ContainsKey(bucket))
        {
            return;
        }

        if (!await BucketExistsAsync(bucket, cancellationToken).ConfigureAwait(false))
        {
            try
            {
                await _client.PutBucketAsync(bucket, cancellationToken).ConfigureAwait(false);
            }
            catch (AmazonS3Exception exception) when (exception.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
            {
                // another instance made it between the check and now
            }
        }

        _knownBuckets[bucket] = true;
    }

    // a listing of one key, which a role that may read the bucket may do; a role that may not read it cannot tell, and the write says what is wrong
    private async Task<bool> BucketExistsAsync(string bucket, CancellationToken cancellationToken)
    {
        try
        {
            await _client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, MaxKeys = 1 }, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (AmazonS3Exception exception) when (exception.ErrorCode == "NoSuchBucket")
        {
            return false;
        }
        catch (AmazonS3Exception)
        {
            return true;
        }
    }

    /// <summary>The body of an object, which ends the response when it is disposed.</summary>
    private sealed class ResponseStream(GetObjectResponse response) : Stream
    {
        private readonly Stream _inner = response.ResponseStream;

        public override bool CanRead => _inner.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => response.ContentLength;

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _inner.ReadAsync(buffer, cancellationToken);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _inner.ReadAsync(buffer, offset, count, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                response.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
