using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Minio;

namespace HsnSoft.Base.BlobStoring.Minio.Tests;

public class MinioRegistrationTests
{
    private static IConfiguration Configuration(params (string Key, string Value)[] settings)
        => new ConfigurationBuilder().AddInMemoryCollection(settings.ToDictionary(x => x.Key, x => (string?)x.Value)).Build();

    [Fact]
    public void MinioSection_IsBoundToTheOptions()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddMinioBlobStoring(Configuration(
            ("Minio:Endpoint", "minio.internal:9000"), ("Minio:AccessKey", "key"), ("Minio:SecretKey", "secret"), ("Minio:UseSsl", "true")));
        using ServiceProvider provider = services.BuildServiceProvider();

        MinioOptions options = provider.GetRequiredService<IOptions<MinioOptions>>().Value;

        Assert.Equal("minio.internal:9000", options.Endpoint);
        Assert.Equal("key", options.AccessKey);
        Assert.Equal("secret", options.SecretKey);
        Assert.True(options.UseSsl);
    }

    [Fact]
    public void TheTimeout_IsNotSetUntilTheServiceSetsIt_AndTheClientIsBuiltEitherWay()
    {
        using ServiceProvider unset = new ServiceCollection().AddLogging().AddMinioBlobStoring(Configuration(("Minio:AccessKey", "key"), ("Minio:SecretKey", "secret"))).BuildServiceProvider();
        using ServiceProvider set = new ServiceCollection().AddLogging().AddMinioBlobStoring(Configuration(("Minio:AccessKey", "key"), ("Minio:SecretKey", "secret"), ("Minio:TimeoutSeconds", "600"))).BuildServiceProvider();

        Assert.Null(unset.GetRequiredService<IOptions<MinioOptions>>().Value.TimeoutSeconds);
        Assert.Equal(600, set.GetRequiredService<IOptions<MinioOptions>>().Value.TimeoutSeconds);
        Assert.NotNull(unset.GetRequiredService<IMinioClient>());
        Assert.NotNull(set.GetRequiredService<IMinioClient>());
    }

    [Fact]
    public void Options_HaveSafeDefaults()
    {
        var options = new MinioOptions();

        Assert.Equal("localhost:9000", options.Endpoint);
        Assert.Equal(string.Empty, options.AccessKey);
        Assert.Equal(string.Empty, options.SecretKey);
        Assert.False(options.UseSsl);
    }

    [Fact]
    public void BlobStoreAndClient_AreResolvable()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddMinioBlobStoring(Configuration(("Minio:AccessKey", "key"), ("Minio:SecretKey", "secret")));
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsType<MinioBlobStore>(provider.GetRequiredService<IBlobStore>());
        Assert.NotNull(provider.GetRequiredService<IMinioClient>());
    }

    [Fact]
    public void ReadinessCheck_IsRegistered()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddMinioBlobStoring(Configuration());
        using ServiceProvider provider = services.BuildServiceProvider();

        HealthCheckRegistration registration = Assert.Single(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("minio", registration.Name);
        Assert.Contains("ready", registration.Tags);
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => MinioServiceCollectionExtensions.AddMinioBlobStoring(null!, Configuration()));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddMinioBlobStoring(null!));
    }
}
