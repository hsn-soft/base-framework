using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.BlobStoring.Azure.Tests;

public class AzureRegistrationTests
{
    private static IConfiguration Configuration(params (string Key, string Value)[] settings)
        => new ConfigurationBuilder().AddInMemoryCollection(settings.ToDictionary(x => x.Key, x => (string?)x.Value)).Build();

    [Fact]
    public void TheSection_IsBoundToTheOptions()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddAzureBlobStoring(Configuration(("AzureBlob:ConnectionString", "UseDevelopmentStorage=true"), ("AzureBlob:ServiceUri", "https://acme.blob.core.windows.net")));
        using ServiceProvider provider = services.BuildServiceProvider();

        AzureBlobOptions options = provider.GetRequiredService<IOptions<AzureBlobOptions>>().Value;

        Assert.Equal("UseDevelopmentStorage=true", options.ConnectionString);
        Assert.Equal("https://acme.blob.core.windows.net", options.ServiceUri);
    }

    [Fact]
    public void Options_AreEmptyByDefault_SoNothingIsReachedByAccident()
    {
        AzureBlobOptions options = new();

        Assert.Null(options.ConnectionString);
        Assert.Null(options.ServiceUri);
    }

    [Fact]
    public void AConnectionString_MakesTheClient_AndTheStore()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddAzureBlobStoring(Configuration(("AzureBlob:ConnectionString", "UseDevelopmentStorage=true")));
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsType<AzureBlobStore>(provider.GetRequiredService<IBlobStore>());
        Assert.Equal("devstoreaccount1", provider.GetRequiredService<BlobServiceClient>().AccountName);
    }

    [Fact]
    public void TheAddressOfTheAccount_IsReachedWithTheIdentityOfTheHost()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddAzureBlobStoring(Configuration(("AzureBlob:ServiceUri", "https://acme.blob.core.windows.net")));
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal("acme", provider.GetRequiredService<BlobServiceClient>().AccountName);
    }

    [Fact]
    public void WithoutAConnectionStringOrAnAddress_TheClientSaysWhatIsMissing()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddAzureBlobStoring(Configuration());
        using ServiceProvider provider = services.BuildServiceProvider();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IBlobStore>());

        Assert.Contains("AzureBlob:ConnectionString or AzureBlob:ServiceUri", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AClientTheHostRegisteredFirst_IsKept()
    {
        BlobServiceClient own = new("UseDevelopmentStorage=true");
        IServiceCollection services = new ServiceCollection().AddLogging().AddSingleton(own).AddAzureBlobStoring(Configuration());
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Same(own, provider.GetRequiredService<BlobServiceClient>());
        Assert.IsType<AzureBlobStore>(provider.GetRequiredService<IBlobStore>());
    }

    [Fact]
    public void ReadinessCheck_IsRegistered()
    {
        IServiceCollection services = new ServiceCollection().AddLogging().AddAzureBlobStoring(Configuration());
        using ServiceProvider provider = services.BuildServiceProvider();

        HealthCheckRegistration registration = Assert.Single(provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("azure-blob", registration.Name);
        Assert.Contains("ready", registration.Tags);
    }

    [Fact]
    public async Task ANullContent_IsRejected_AndNullArgumentsToTheRegistration()
    {
        AzureBlobStore store = new(new BlobServiceClient("UseDevelopmentStorage=true"));

        await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveAsync("c", "n", null!, "text/plain"));
        Assert.Throws<ArgumentNullException>(() => AzureBlobServiceCollectionExtensions.AddAzureBlobStoring(null!, Configuration()));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddAzureBlobStoring(null!));
    }
}
