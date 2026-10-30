using HsnSoft.Base.BlobStoring.Azure;
using Microsoft.Extensions.Configuration;

namespace HsnSoft.Base.IntegrationTests.BlobStoring;

/// <summary>The Azure Blob Storage client against Azurite, the emulator of Microsoft.</summary>
[Collection(Collections.Azurite)]
public sealed class AzureBlobTests(AzuriteFixture azurite) : BlobStoreContractTests
{
    protected override async Task<IHost> StartAsync()
    {
        IConfiguration configuration = Support.Configuration(("AzureBlob:ConnectionString", azurite.Container.GetConnectionString()));
        return await Support.StartHostAsync(services => services.AddAzureBlobStoring(configuration));
    }

    protected override Task StopAsync() => Task.CompletedTask;
}
