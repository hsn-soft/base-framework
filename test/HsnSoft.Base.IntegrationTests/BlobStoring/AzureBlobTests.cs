using HsnSoft.Base.BlobStoring.Azure;
using Microsoft.Extensions.Configuration;
using Testcontainers.Azurite;

namespace HsnSoft.Base.IntegrationTests.BlobStoring;

/// <summary>The Azure Blob Storage client against Azurite, the emulator of Microsoft.</summary>
public sealed class AzureBlobTests : BlobStoreContractTests
{
    private readonly AzuriteContainer _container = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:3.35.0")
        .WithCommand("--skipApiVersionCheck")
        .Build();

    protected override async Task<IHost> StartAsync()
    {
        await _container.StartAsync();
        IConfiguration configuration = Support.Configuration(("AzureBlob:ConnectionString", _container.GetConnectionString()));
        return await Support.StartHostAsync(services => services.AddAzureBlobStoring(configuration));
    }

    protected override async Task StopAsync() => await Support.DisposeAsync(_container);
}
