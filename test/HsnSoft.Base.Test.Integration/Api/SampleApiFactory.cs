using HsnSoft.Base.Test.Integration.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace HsnSoft.Base.Test.Integration.Api;

/// <summary>
/// Boots the real sample microservice (Program) in-process, wired to throw-away PostgreSQL, MongoDB, RabbitMQ and Redis containers.
/// </summary>
[CollectionDefinition(Name)]
public class SampleApiCollection : ICollectionFixture<SampleApiFactory>
{
    public const string Name = "SampleApi";
}

public class SampleApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16").Build();
    private readonly MongoDbContainer _mongo = new MongoDbBuilder("mongo:latest").Build();
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:4-management").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:latest").Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _mongo.StartAsync(), _rabbitMq.StartAsync(), _redis.StartAsync());

        var mongoUrl = new MongoUrlBuilder(_mongo.GetConnectionString()) { DatabaseName = "sample", AuthenticationSource = "admin" }.ToMongoUrl();

        // Environment variables are read by WebApplication.CreateBuilder before any factory hook runs.
        Environment.SetEnvironmentVariable("ConnectionStrings__EfCore-PerformanceService", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__Mongo-PerformanceService", mongoUrl.ToString());
        Environment.SetEnvironmentVariable("RabbitMq__Connection__HostName", _rabbitMq.Hostname);
        Environment.SetEnvironmentVariable("RabbitMq__Connection__Port", _rabbitMq.GetMappedPublicPort(RabbitMqBuilder.RabbitMqPort).ToString());
        Environment.SetEnvironmentVariable("RabbitMq__Connection__UserName", RabbitMqBuilder.DefaultUsername);
        Environment.SetEnvironmentVariable("RabbitMq__Connection__Password", RabbitMqBuilder.DefaultPassword);
        Environment.SetEnvironmentVariable("Redis__Configuration", _redis.GetConnectionString());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _mongo.DisposeAsync().AsTask(), _rabbitMq.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync();
}
