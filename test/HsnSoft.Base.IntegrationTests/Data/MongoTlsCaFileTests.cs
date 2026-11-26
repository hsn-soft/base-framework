using HsnSoft.Base.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HsnSoft.Base.IntegrationTests.Data;

/// <summary>
/// The <c>tlsCAFile</c> of the connection string against a MongoDB that talks TLS with a certificate of a private CA. The driver ignores the option; <see cref="MongoOptions.Apply"/> makes it work (the check that the unit tests make of the certificate
/// is here made by a real handshake).
/// </summary>
[Collection(Collections.MongoTls)]
public sealed class MongoTlsCaFileTests(MongoTlsFixture mongo) : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("mongo-tls-").FullName;

    public void Dispose() => Directory.Delete(_folder, true);

    private string File(string name, string pem)
    {
        string path = Path.Combine(_folder, name);
        System.IO.File.WriteAllText(path, pem);
        return path;
    }

    private string Url(string options) => $"mongodb://{mongo.HostAndPort}/tls?directConnection=true&tls=true&serverSelectionTimeoutMS=4000&connectTimeoutMS=4000{options}";

    private static async Task PingAsync(string connectionString)
    {
        using MongoClient client = new(new MongoOptions().Apply(connectionString));
        await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
    }

    [Fact]
    public async Task TheCaOfTheServer_IsTrusted_AndTheConnectionWorks()
    {
        string ca = File("ca.pem", mongo.CaPem);

        await PingAsync(Url($"&tlsCAFile={Uri.EscapeDataString(ca)}"));
    }

    [Fact]
    public async Task AnotherCa_IsRefused()
    {
        string other = File("other.pem", mongo.OtherCaPem);

        await Assert.ThrowsAnyAsync<Exception>(() => PingAsync(Url($"&tlsCAFile={Uri.EscapeDataString(other)}")));
    }

    [Fact]
    public async Task WithoutTheCaFile_TheSystemDoesNotTrustTheServer()
        => await Assert.ThrowsAnyAsync<Exception>(() => PingAsync(Url(string.Empty)));

    [Fact]
    public async Task TlsInsecure_StillTurnsTheCheckOff()
        => await PingAsync(Url("&tlsInsecure=true"));
}
