using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MongoDB.Driver;
using Testcontainers.Azurite;
using Testcontainers.Kafka;
using Testcontainers.Minio;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace HsnSoft.Base.IntegrationTests;

/// <summary>
/// One container for every test class that needs the same service: a container takes seconds to start and to remove, and a run of the tests started seventy of them. The classes of a collection run one after the other (the assembly does not run tests
/// in parallel), and **a test never shares what it changes**: it makes its own database, bucket, exchange or topic with a name nobody else has (<c>NewConnectionString</c>, <c>NewUrl</c>, a new <c>Guid</c> in a name). A test that changes the container itself
/// (stops it, pauses it, cuts its network) does not use a fixture: it starts a container of its own, so the others never see it broken.
/// </summary>
public abstract class ContainerFixture<TContainer> : IAsyncLifetime
    where TContainer : IContainer
{
    public TContainer Container { get; private set; } = default!;

    protected abstract TContainer Create();

    public async Task InitializeAsync()
    {
        Container = Create();
        await Container.StartAsync();
    }

    public Task DisposeAsync() => Support.DisposeAsync(Container);
}

/// <summary>A relational server: every test asks for a connection string of a database of its own (the context creates the database when it is not there).</summary>
public abstract class SqlFixture<TContainer> : ContainerFixture<TContainer>
    where TContainer : IContainer, IDatabaseContainer
{
    public string NewConnectionString()
        => Regex.Replace(Container.GetConnectionString(), "Database=[^;]*", $"Database=t{Guid.NewGuid():N}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));
}

public sealed class PostgresFixture : SqlFixture<PostgreSqlContainer>
{
    protected override PostgreSqlContainer Create() => new PostgreSqlBuilder("postgres:16").Build();
}

public sealed class MySqlFixture : SqlFixture<MySqlContainer>
{
    // root: a test makes a database of its own, which the default user of the image is not allowed to
    protected override MySqlContainer Create() => new MySqlBuilder("mysql:8.4").WithUsername("root").Build();
}

public sealed class SqlServerFixture : SqlFixture<MsSqlContainer>
{
    protected override MsSqlContainer Create() => new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
}

public sealed class MongoFixture : ContainerFixture<MongoDbContainer>
{
    // the server looks for expired documents every minute by default; every second here, so the test that waits for it does not wait a minute
    protected override MongoDbContainer Create() => new MongoDbBuilder("mongo:7.0").WithCommand("--setParameter", "ttlMonitorSleepSecs=1").Build();

    /// <summary>The url of a database no other test has.</summary>
    public string NewUrl() => new MongoUrlBuilder(Container.GetConnectionString()) { DatabaseName = $"t{Guid.NewGuid():N}", AuthenticationSource = "admin" }.ToString();
}

/// <summary>A MongoDB that is a replica set (of one member): the only kind that has transactions. The tests of the transactions of the repository use it; the others use the standalone <see cref="MongoFixture"/>.</summary>
public sealed class MongoReplicaSetFixture : ContainerFixture<MongoDbContainer>
{
    protected override MongoDbContainer Create() => new MongoDbBuilder("mongo:7.0").WithReplicaSet().Build();

    /// <summary>The url of a database no other test has.</summary>
    public string NewUrl() => new MongoUrlBuilder(Container.GetConnectionString()) { DatabaseName = $"t{Guid.NewGuid():N}", AuthenticationSource = "admin" }.ToString();
}

public sealed class RedisFixture : ContainerFixture<RedisContainer>
{
    protected override RedisContainer Create() => new RedisBuilder("redis:7").Build();
}

public sealed class MinioFixture : ContainerFixture<MinioContainer>
{
    protected override MinioContainer Create() => new MinioBuilder(Support.MinioImage).Build();
}

public sealed class AzuriteFixture : ContainerFixture<AzuriteContainer>
{
    protected override AzuriteContainer Create() => new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:3.35.0").WithCommand("--skipApiVersionCheck").Build();
}

public sealed class KafkaFixture : ContainerFixture<KafkaContainer>
{
    protected override KafkaContainer Create() => new KafkaBuilder("confluentinc/cp-kafka:7.8.0").Build();
}

/// <summary>
/// A MongoDB that only talks TLS (<c>requireTLS</c>), with a server certificate signed by a private CA that is made for the test run and exists nowhere else: the system does not trust it, so only the CA file of the connection string (<c>tlsCAFile</c>) can.
/// <see cref="CaPem"/> is the CA that signed it, <see cref="OtherCaPem"/> a CA that did not.
/// </summary>
public sealed class MongoTlsFixture : ContainerFixture<IContainer>
{
    private const int MongoPort = 27017;

    public string CaPem { get; private set; } = string.Empty;

    public string OtherCaPem { get; private set; } = string.Empty;

    /// <summary>The host and port the tests connect to; <c>localhost</c> is in the certificate.</summary>
    public string HostAndPort => $"localhost:{Container.GetMappedPublicPort(MongoPort)}";

    protected override IContainer Create()
    {
        (string ca, string server) = MakeCertificates();
        CaPem = ca;
        OtherCaPem = MakeCertificates().Ca;
        return new ContainerBuilder("mongo:7.0")
            .WithPortBinding(MongoPort, true)
            .WithResourceMapping(Encoding.ASCII.GetBytes(server), "/certs/server.pem", 0b110_100_100 /* rw-r--r--: the server runs as another user than the one that owns the file */)
            .WithResourceMapping(Encoding.ASCII.GetBytes(ca), "/certs/ca.pem", 0b110_100_100)
            .WithCommand("mongod", "--tlsMode", "requireTLS", "--tlsCertificateKeyFile", "/certs/server.pem", "--tlsCAFile", "/certs/ca.pem", "--tlsAllowConnectionsWithoutCertificates", "--bind_ip_all")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Waiting for connections"))
            .Build();
    }

    /// <summary>A new CA (PEM) and a server certificate and key (PEM, one file) for localhost that it signed.</summary>
    private static (string Ca, string Server) MakeCertificates()
    {
        using RSA caKey = RSA.Create(2048);
        CertificateRequest caRequest = new("CN=HsnSoft test CA", caKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        caRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        using X509Certificate2 ca = caRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(30));

        using RSA serverKey = RSA.Create(2048);
        CertificateRequest request = new("CN=localhost", serverKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        SubjectAlternativeNameBuilder names = new();
        names.AddDnsName("localhost");
        names.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(names.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], false));
        byte[] serial = RandomNumberGenerator.GetBytes(8);
        serial[0] &= 0x7F;
        using X509Certificate2 server = request.Create(ca, DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddDays(10), serial);

        return (ca.ExportCertificatePem() + "\n", server.ExportCertificatePem() + "\n" + serverKey.ExportPkcs8PrivateKeyPem() + "\n");
    }
}

/// <summary>The names of the collections: the classes of one collection share the container of its fixture.</summary>
public static class Collections
{
    public const string Postgres = "postgres";
    public const string MySql = "mysql";
    public const string SqlServer = "sqlserver";
    public const string Mongo = "mongo";
    public const string MongoReplicaSet = "mongo-replica-set";
    public const string MongoTls = "mongo-tls";
    public const string Redis = "redis";
    public const string Minio = "minio";
    public const string Azurite = "azurite";
    public const string Kafka = "kafka";
}

[CollectionDefinition(Collections.Postgres)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;

[CollectionDefinition(Collections.MySql)]
public sealed class MySqlCollection : ICollectionFixture<MySqlFixture>;

[CollectionDefinition(Collections.SqlServer)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>;

[CollectionDefinition(Collections.Mongo)]
public sealed class MongoCollection : ICollectionFixture<MongoFixture>;

[CollectionDefinition(Collections.MongoReplicaSet)]
public sealed class MongoReplicaSetCollection : ICollectionFixture<MongoReplicaSetFixture>;

[CollectionDefinition(Collections.MongoTls)]
public sealed class MongoTlsCollection : ICollectionFixture<MongoTlsFixture>;

[CollectionDefinition(Collections.Redis)]
public sealed class RedisCollection : ICollectionFixture<RedisFixture>;

[CollectionDefinition(Collections.Minio)]
public sealed class MinioCollection : ICollectionFixture<MinioFixture>;

[CollectionDefinition(Collections.Azurite)]
public sealed class AzuriteCollection : ICollectionFixture<AzuriteFixture>;

[CollectionDefinition(Collections.Kafka)]
public sealed class KafkaCollection : ICollectionFixture<KafkaFixture>;
