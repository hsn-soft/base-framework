using System.Text.RegularExpressions;
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

/// <summary>The names of the collections: the classes of one collection share the container of its fixture.</summary>
public static class Collections
{
    public const string Postgres = "postgres";
    public const string MySql = "mysql";
    public const string SqlServer = "sqlserver";
    public const string Mongo = "mongo";
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

[CollectionDefinition(Collections.Redis)]
public sealed class RedisCollection : ICollectionFixture<RedisFixture>;

[CollectionDefinition(Collections.Minio)]
public sealed class MinioCollection : ICollectionFixture<MinioFixture>;

[CollectionDefinition(Collections.Azurite)]
public sealed class AzuriteCollection : ICollectionFixture<AzuriteFixture>;

[CollectionDefinition(Collections.Kafka)]
public sealed class KafkaCollection : ICollectionFixture<KafkaFixture>;
