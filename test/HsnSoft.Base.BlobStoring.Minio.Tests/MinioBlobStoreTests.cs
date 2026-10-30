using System.Reflection;
using System.Security.Cryptography;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace HsnSoft.Base.BlobStoring.Minio.Tests;

public class MinioBlobStoreTests
{
    /// <summary>An <see cref="IMinioClient"/> that answers "no such bucket" and refuses to create it, as when another instance won the race.</summary>
    public class RacingClient : DispatchProxy
    {
        public List<string> Calls { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            string name = targetMethod!.Name;
            Calls.Add(name);

            if (name == "MakeBucketAsync")
            {
                throw new MinioException("bucket already exists");
            }

            object? result = name == "BucketExistsAsync" ? false : null;
            return targetMethod.ReturnType == typeof(Task)
                ? Task.CompletedTask
                : typeof(Task).GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(targetMethod.ReturnType.GetGenericArguments()[0])
                    .Invoke(null, [result]);
        }
    }

    [Fact]
    public async Task BucketCreatedByAnotherInstanceMeanwhile_DoesNotFailTheSave()
    {
        IMinioClient client = DispatchProxy.Create<IMinioClient, RacingClient>();
        var store = new MinioBlobStore(client);

        await store.SaveAsync("shared", "a.txt", new MemoryStream("x"u8.ToArray()), "text/plain");
        await store.SaveAsync("shared", "b.txt", new MemoryStream("y"u8.ToArray()), "text/plain");

        List<string> calls = ((RacingClient)(object)client).Calls;
        Assert.Equal(1, calls.Count(call => call == "MakeBucketAsync"));
        Assert.Equal(2, calls.Count(call => call == "PutObjectAsync"));
    }

    [Fact]
    public async Task NullContent_IsRejected()
    {
        var store = new MinioBlobStore(DispatchProxy.Create<IMinioClient, RacingClient>());

        await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveAsync("b", "n", null!, "text/plain"));
    }

    // ---- the read: an object is read while it arrives ----

    /// <summary>An <see cref="IMinioClient"/> whose download hands the callback a body that the test makes (the client of MinIO gives the body to a callback and not to the caller).</summary>
    public class DownloadingClient : DispatchProxy
    {
        public Func<Func<Stream, CancellationToken, Task>, CancellationToken, Task>? OnGet { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name != "GetObjectAsync")
            {
                throw new NotSupportedException(targetMethod.Name);
            }

            Func<Stream, CancellationToken, Task> callback = (Func<Stream, CancellationToken, Task>)typeof(GetObjectArgs).GetProperty("CallBack", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetGetMethod(true)!.Invoke(args![0], null)!;
            return Run(callback, (CancellationToken)args[1]!);
        }

        private async Task<ObjectStat> Run(Func<Stream, CancellationToken, Task> callback, CancellationToken token)
        {
            await OnGet!(callback, token);
            return null!;
        }
    }

    /// <summary>The body of an object: its bytes are made as they are read, and how many were made is known.</summary>
    private sealed class Body(long length, long failAfter = -1) : Stream
    {
        private long _made;

        public long Made => Interlocked.Read(ref _made);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => length;

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
            if (failAfter >= 0 && _made >= failAfter)
            {
                throw new IOException("the connection to the store broke");
            }

            int toMake = (int)Math.Min(count, length - _made);
            for (int index = 0; index < toMake; index++)
            {
                buffer[offset + index] = (byte)((_made + index) % 251);
            }

            Interlocked.Add(ref _made, toMake);
            return toMake;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private static (MinioBlobStore Store, DownloadingClient Client) StoreWith(Func<Func<Stream, CancellationToken, Task>, CancellationToken, Task> onGet)
    {
        IMinioClient client = DispatchProxy.Create<IMinioClient, DownloadingClient>();
        DownloadingClient fake = (DownloadingClient)(object)client;
        fake.OnGet = onGet;
        return (new MinioBlobStore(client), fake);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AnObjectOrABucketThatIsNotThere_ReadsAsNull(bool theBucket)
    {
        (MinioBlobStore store, _) = StoreWith((_, _) => throw (theBucket ? new BucketNotFoundException("b", "no such bucket") : new ObjectNotFoundException("o", "no such object")));

        Assert.Null(await store.OpenReadAsync("b", "o"));
    }

    [Fact]
    public async Task AnObject_IsReadWhole_AndInOrder_WhileItArrives()
    {
        const long size = (10 * 1024 * 1024) + 321;
        (MinioBlobStore store, _) = StoreWith(async (callback, token) => await callback(new Body(size), token));

        await using Stream stream = (await store.OpenReadAsync("b", "o"))!;

        Assert.False(stream.CanSeek);
        Assert.Equal(SHA256.HashData(Expected(size)), await SHA256.HashDataAsync(stream));
    }

    [Fact]
    public async Task AnEmptyObject_IsAnEmptyStream()
    {
        (MinioBlobStore store, _) = StoreWith(async (callback, token) => await callback(new Body(0), token));

        await using Stream stream = (await store.OpenReadAsync("b", "o"))!;

        Assert.Equal(0, await stream.ReadAsync(new byte[10]));
    }

    [Fact]
    public async Task ALargeObject_IsNotDownloadedAheadOfTheReader_BeyondTheWindowOfThePipe()
    {
        Body body = new(512L * 1024 * 1024);
        TaskCompletionSource ended = new(TaskCreationOptions.RunContinuationsAsynchronously);
        (MinioBlobStore store, _) = StoreWith(async (callback, token) =>
        {
            try
            {
                await callback(body, token);
            }
            finally
            {
                ended.TrySetResult();
            }
        });

        await using Stream stream = (await store.OpenReadAsync("b", "o"))!;
        byte[] first = new byte[1024];
        await stream.ReadExactlyAsync(first);
        await Task.Delay(500);

        // the reader read a kilobyte: the download is waiting at the window of the pipe, not at the end of 512 MB
        Assert.True(body.Made < MinioDownload.PauseAt + (4 * 1024 * 1024), $"{body.Made} bytes of the object were made for a reader that read 1 KB");
        Assert.Equal(Expected(1024), first);
        Assert.False(ended.Task.IsCompleted);
    }

    [Fact]
    public async Task DisposingTheStream_EndsTheDownload()
    {
        TaskCompletionSource<bool> cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        (MinioBlobStore store, _) = StoreWith(async (callback, token) =>
        {
            try
            {
                await callback(new Body(512L * 1024 * 1024), token);
                cancelled.TrySetResult(false);
            }
            catch (OperationCanceledException)
            {
                cancelled.TrySetResult(true);
                throw;
            }
        });
        Stream stream = (await store.OpenReadAsync("b", "o"))!;
        await stream.ReadExactlyAsync(new byte[10]);

        await stream.DisposeAsync();
        await stream.DisposeAsync();

        Assert.True(await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public async Task ADisposeWithoutAwait_AlsoEndsTheDownload()
    {
        TaskCompletionSource<bool> cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        (MinioBlobStore store, _) = StoreWith(async (callback, token) =>
        {
            try
            {
                await callback(new Body(512L * 1024 * 1024), token);
                cancelled.TrySetResult(false);
            }
            catch (OperationCanceledException)
            {
                cancelled.TrySetResult(true);
                throw;
            }
        });
        Stream stream = (await store.OpenReadAsync("b", "o"))!;
        await stream.ReadExactlyAsync(new byte[10]);

        stream.Dispose();

        Assert.True(await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public async Task AFailureBeforeTheFirstBytes_IsThrownWhenTheObjectIsOpened()
    {
        (MinioBlobStore store, _) = StoreWith((_, _) => throw new IOException("the store cannot be reached"));

        await Assert.ThrowsAsync<IOException>(() => store.OpenReadAsync("b", "o"));
    }

    [Fact]
    public async Task AFailureAfterTheFirstBytes_IsThrownByTheRead_NotHidden()
    {
        (MinioBlobStore store, _) = StoreWith(async (callback, token) => await callback(new Body(10_000_000, failAfter: 200_000), token));
        await using Stream stream = (await store.OpenReadAsync("b", "o"))!;

        await Assert.ThrowsAsync<IOException>(async () =>
        {
            byte[] buffer = new byte[64 * 1024];
            while (await stream.ReadAsync(buffer) > 0)
            {
            }
        });
    }

    [Fact]
    public async Task ACancellationWhileTheObjectIsBeingOpened_StopsIt()
    {
        (MinioBlobStore store, _) = StoreWith(async (_, token) => await Task.Delay(Timeout.Infinite, token));
        using CancellationTokenSource cancel = new(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.OpenReadAsync("b", "o", cancel.Token));
    }

    [Fact]
    public async Task TheStream_ReadsInEveryWay_AndRefusesWhatItCannotDo()
    {
        (MinioBlobStore store, _) = StoreWith(async (callback, token) => await callback(new Body(5000), token));
        await using Stream stream = (await store.OpenReadAsync("b", "o"))!;
        byte[] buffer = new byte[100];

        Assert.True(stream.CanRead);
        Assert.False(stream.CanWrite);
        Assert.Equal(100, stream.Read(buffer, 0, 100));
#pragma warning disable CA1835 // the array overload is the one under test
        Assert.Equal(100, await stream.ReadAsync(buffer, 0, 100, CancellationToken.None));
#pragma warning restore CA1835
        stream.Flush();
        Assert.Throws<NotSupportedException>(() => stream.Length);
        Assert.Throws<NotSupportedException>(() => stream.Position);
        Assert.Throws<NotSupportedException>(() => stream.Position = 1);
        Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        Assert.Throws<NotSupportedException>(() => stream.SetLength(1));
        Assert.Throws<NotSupportedException>(() => stream.Write(buffer, 0, 1));
    }
}
