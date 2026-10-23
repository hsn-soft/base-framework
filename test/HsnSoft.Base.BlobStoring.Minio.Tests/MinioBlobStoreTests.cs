using System.Reflection;
using Minio;
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
            var name = targetMethod!.Name;
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
        var client = DispatchProxy.Create<IMinioClient, RacingClient>();
        var store = new MinioBlobStore(client);

        await store.SaveAsync("shared", "a.txt", new MemoryStream("x"u8.ToArray()), "text/plain");
        await store.SaveAsync("shared", "b.txt", new MemoryStream("y"u8.ToArray()), "text/plain");

        var calls = ((RacingClient)(object)client).Calls;
        Assert.Equal(1, calls.Count(call => call == "MakeBucketAsync"));
        Assert.Equal(2, calls.Count(call => call == "PutObjectAsync"));
    }

    [Fact]
    public async Task NullContent_IsRejected()
    {
        var store = new MinioBlobStore(DispatchProxy.Create<IMinioClient, RacingClient>());

        await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveAsync("b", "n", null!, "text/plain"));
    }
}
