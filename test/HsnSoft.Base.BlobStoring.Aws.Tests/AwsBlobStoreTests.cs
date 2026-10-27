#pragma warning disable CA1835 // the test reads with every overload the stream has
using System.Net;
using System.Reflection;
using Amazon.S3;
using Amazon.S3.Model;

namespace HsnSoft.Base.BlobStoring.Aws.Tests;

public class AwsBlobStoreTests
{
    /// <summary>An <see cref="IAmazonS3"/> that answers "no such bucket" and refuses to make it, as when another instance won the race, and "not found" for what is read or deleted.</summary>
    public class RacingClient : DispatchProxy
    {
        public List<string> Calls { get; } = [];

        public List<PutObjectRequest> Puts { get; } = [];

        public List<bool> Seekable { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            string name = targetMethod!.Name;
            Calls.Add(name);

            switch (name)
            {
                case "ListObjectsV2Async":
                    throw new AmazonS3Exception("no such bucket", Amazon.Runtime.ErrorType.Sender, "NoSuchBucket", "id", HttpStatusCode.NotFound);
                case "PutBucketAsync":
                    throw new AmazonS3Exception("already made", Amazon.Runtime.ErrorType.Sender, "BucketAlreadyOwnedByYou", "id", HttpStatusCode.Conflict);
                case "GetObjectAsync":
                case "DeleteObjectAsync":
                    throw new AmazonS3Exception("not found", Amazon.Runtime.ErrorType.Sender, "NoSuchBucket", "id", HttpStatusCode.NotFound);
                case "PutObjectAsync":
                    Puts.Add((PutObjectRequest)args![0]!);
                    Seekable.Add(Puts[^1].InputStream.CanSeek);
                    return Task.FromResult(new PutObjectResponse());
            }

            return targetMethod.ReturnType == typeof(Task)
                ? Task.CompletedTask
                : typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(targetMethod.ReturnType.GetGenericArguments()[0]).Invoke(null, [null]);
        }
    }

    /// <summary>An <see cref="IAmazonS3"/> whose bucket is there and has one object.</summary>
    public class ThereClient : DispatchProxy
    {
        public List<string> Calls { get; } = [];

        public GetObjectResponse? Response { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Calls.Add(targetMethod!.Name);
            object? result = targetMethod.Name switch
            {
                "GetObjectAsync" => Response,
                "ListObjectsV2Async" => new ListObjectsV2Response(),
                "PutObjectAsync" => new PutObjectResponse(),
                _ => null,
            };
            return targetMethod.ReturnType == typeof(Task)
                ? Task.CompletedTask
                : typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(targetMethod.ReturnType.GetGenericArguments()[0]).Invoke(null, [result]);
        }
    }

    [Fact]
    public async Task ABucketThatIsThere_IsNotMade_AndABlobIsReadThroughAStreamThatEndsTheResponse()
    {
        MemoryStream body = new("hello"u8.ToArray());
        IAmazonS3 proxy = DispatchProxy.Create<IAmazonS3, ThereClient>();
        ThereClient client = (ThereClient)(object)proxy;
        client.Response = new GetObjectResponse { ResponseStream = body };
        AwsBlobStore store = new(proxy);

        await store.SaveAsync("there", "a.txt", new MemoryStream("x"u8.ToArray()), "text/plain");
        Stream stream = (await store.OpenReadAsync("there", "a.txt"))!;

        Assert.DoesNotContain("PutBucketAsync", client.Calls);
        Assert.True(stream.CanRead);
        Assert.False(stream.CanSeek);
        Assert.False(stream.CanWrite);
        byte[] first = new byte[2];
        Assert.Equal(2, stream.Read(first, 0, 2));
        Assert.Equal(2, await stream.ReadAsync(first.AsMemory()));
        Assert.Equal(1, await stream.ReadAsync(first, 0, 2, CancellationToken.None));
        Assert.Throws<NotSupportedException>(() => stream.Position);
        Assert.Throws<NotSupportedException>(() => stream.Position = 0);
        Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
        Assert.Throws<NotSupportedException>(() => stream.Write(first, 0, 1));
        stream.Flush();
        _ = stream.Length;
        await stream.DisposeAsync();
        Assert.False(body.CanRead);
    }

    private static (AwsBlobStore Store, RacingClient Client) NewStore()
    {
        IAmazonS3 proxy = DispatchProxy.Create<IAmazonS3, RacingClient>();
        return (new AwsBlobStore(proxy), (RacingClient)(object)proxy);
    }

    [Fact]
    public async Task ABucketMadeByAnotherInstanceMeanwhile_DoesNotFailTheSave_AndIsMadeOnce()
    {
        (AwsBlobStore store, RacingClient client) = NewStore();

        await store.SaveAsync("shared", "a.txt", new MemoryStream("x"u8.ToArray()), "text/plain");
        await store.SaveAsync("shared", "b.txt", new MemoryStream("y"u8.ToArray()), "text/plain");

        Assert.Equal(1, client.Calls.Count(call => call == "PutBucketAsync"));
        Assert.Equal(["a.txt", "b.txt"], client.Puts.Select(put => put.Key));
        Assert.All(client.Puts, put => Assert.Equal("text/plain", put.ContentType));
    }

    [Fact]
    public async Task AStreamThatCannotSeek_IsBufferedSoTheSizeIsKnown()
    {
        (AwsBlobStore store, RacingClient client) = NewStore();

        await store.SaveAsync("b", "n", new ForwardOnlyStream("streamed"u8.ToArray()), "text/plain");

        Assert.True(Assert.Single(client.Seekable));
    }

    [Fact]
    public async Task AMissingBucketOrBlob_ReadsAsNull_AndDeletingFromItIsFine()
    {
        (AwsBlobStore store, _) = NewStore();

        Assert.Null(await store.OpenReadAsync("none", "n"));
        await store.DeleteAsync("none", "n");
    }

    [Fact]
    public async Task ABucketThatCannotBeListed_IsTakenAsThere_AndIsNotMade()
    {
        IAmazonS3 proxy = DispatchProxy.Create<IAmazonS3, DeniedClient>();
        AwsBlobStore store = new(proxy);

        await store.SaveAsync("locked", "a.txt", new MemoryStream("x"u8.ToArray()), "text/plain");

        Assert.DoesNotContain("PutBucketAsync", ((DeniedClient)(object)proxy).Calls);
    }

    public class DeniedClient : DispatchProxy
    {
        public List<string> Calls { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Calls.Add(targetMethod!.Name);
            if (targetMethod.Name == "ListObjectsV2Async")
            {
                throw new AmazonS3Exception("denied", Amazon.Runtime.ErrorType.Sender, "AccessDenied", "id", HttpStatusCode.Forbidden);
            }

            return targetMethod.ReturnType == typeof(Task)
                ? Task.CompletedTask
                : typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(targetMethod.ReturnType.GetGenericArguments()[0]).Invoke(null, [targetMethod.Name == "PutObjectAsync" ? new PutObjectResponse() : null]);
        }
    }

    [Fact]
    public async Task ANullContent_IsRejected()
    {
        (AwsBlobStore store, _) = NewStore();

        await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveAsync("b", "n", null!, "text/plain"));
    }

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
}
