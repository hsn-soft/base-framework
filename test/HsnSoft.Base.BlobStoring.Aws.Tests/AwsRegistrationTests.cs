using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.BlobStoring.Aws.Tests;

public class AwsRegistrationTests
{
    private static IConfiguration Configuration(params (string Key, string Value)[] settings)
        => new ConfigurationBuilder().AddInMemoryCollection(settings.ToDictionary(x => x.Key, x => (string?)x.Value)).Build();

    private static ServiceProvider Build(params (string Key, string Value)[] settings)
        => new ServiceCollection().AddLogging().AddAwsBlobStoring(Configuration(settings)).BuildServiceProvider();

    [Fact]
    public void TheSection_IsBoundToTheOptions()
    {
        using ServiceProvider provider = Build(("AwsS3:Region", "eu-central-1"), ("AwsS3:ServiceUrl", "http://s3.internal:9000"), ("AwsS3:AccessKey", "key"), ("AwsS3:SecretKey", "secret"), ("AwsS3:ForcePathStyle", "false"));

        AwsBlobOptions options = provider.GetRequiredService<IOptions<AwsBlobOptions>>().Value;

        Assert.Equal(("eu-central-1", "http://s3.internal:9000", "key", "secret", false), (options.Region, options.ServiceUrl, options.AccessKey, options.SecretKey, options.ForcePathStyle));
    }

    [Fact]
    public void TheTimeoutAndTheRetries_AreTheSdksOwn_UntilTheyAreSet()
    {
        using ServiceProvider unset = Build(("AwsS3:Region", "eu-central-1"));
        using ServiceProvider set = Build(("AwsS3:Region", "eu-central-1"), ("AwsS3:TimeoutSeconds", "600"), ("AwsS3:MaxErrorRetry", "5"));

        IClientConfig plain = unset.GetRequiredService<IAmazonS3>().Config;
        IClientConfig configured = set.GetRequiredService<IAmazonS3>().Config;

        // no total time for a request is set unless the service says so: an upload of a large file would be cut by one
        Assert.True(plain.Timeout > TimeSpan.FromDays(1));
        Assert.Equal(2, plain.MaxErrorRetry);
        Assert.Equal(TimeSpan.FromSeconds(600), configured.Timeout);
        Assert.Equal(5, configured.MaxErrorRetry);
    }

    [Fact]
    public void OnAws_OnlyTheRegionIsGiven_AndTheCredentialsComeFromTheHost()
    {
        using ServiceProvider provider = Build(("AwsS3:Region", "eu-central-1"));

        IAmazonS3 client = provider.GetRequiredService<IAmazonS3>();

        Assert.IsType<AwsBlobStore>(provider.GetRequiredService<IBlobStore>());
        Assert.Equal("eu-central-1", client.Config.RegionEndpoint.SystemName);
        Assert.False(((AmazonS3Config)client.Config).ForcePathStyle);
    }

    [Fact]
    public void AnS3CompatibleStore_IsReachedByItsUrl_WithTheBucketInThePath()
    {
        using ServiceProvider provider = Build(("AwsS3:ServiceUrl", "http://localhost:9000"), ("AwsS3:AccessKey", "key"), ("AwsS3:SecretKey", "secret"));

        AmazonS3Config config = (AmazonS3Config)provider.GetRequiredService<IAmazonS3>().Config;

        Assert.Equal("http://localhost:9000/", config.ServiceURL);
        Assert.True(config.ForcePathStyle);
        Assert.Equal("us-east-1", config.AuthenticationRegion);
    }

    [Fact]
    public void ARegionWithAUrl_IsTheRegionOfTheSignature_AndThePathStyleCanBeTurnedOff()
    {
        using ServiceProvider provider = Build(("AwsS3:ServiceUrl", "http://localhost:9000"), ("AwsS3:Region", "eu-west-1"), ("AwsS3:ForcePathStyle", "false"));

        AmazonS3Config config = (AmazonS3Config)provider.GetRequiredService<IAmazonS3>().Config;

        Assert.Equal("eu-west-1", config.AuthenticationRegion);
        Assert.False(config.ForcePathStyle);
    }

    [Fact]
    public void AClientTheHostRegisteredFirst_IsKept()
    {
        IAmazonS3 own = new AmazonS3Client(new AmazonS3Config { ServiceURL = "http://own:9000" });
        using ServiceProvider provider = new ServiceCollection().AddLogging().AddSingleton(own).AddAwsBlobStoring(Configuration()).BuildServiceProvider();

        Assert.Same(own, provider.GetRequiredService<IAmazonS3>());
    }

    [Fact]
    public void ReadinessCheck_IsRegistered()
    {
        using ServiceProvider provider = Build();

        HealthCheckRegistration registration = Assert.Single(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("aws-s3", registration.Name);
        Assert.Contains("ready", registration.Tags);
    }

    [Fact]
    public void NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => AwsBlobServiceCollectionExtensions.AddAwsBlobStoring(null!, Configuration()));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddAwsBlobStoring(null!));
    }
}
