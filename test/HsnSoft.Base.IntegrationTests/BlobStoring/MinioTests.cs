using HsnSoft.Base.BlobStoring.Minio;
using Microsoft.Extensions.Configuration;
using Testcontainers.Minio;

namespace HsnSoft.Base.IntegrationTests.BlobStoring;

public sealed class MinioTests : BlobStoreContractTests
{
    private readonly MinioContainer _container = new MinioBuilder(Support.MinioImage).Build();

    protected override async Task<IHost> StartAsync()
    {
        await _container.StartAsync();
        IConfiguration configuration = Support.Configuration(
            ("Minio:Endpoint", $"{_container.Hostname}:{_container.GetMappedPublicPort(9000)}"),
            ("Minio:AccessKey", _container.GetAccessKey()),
            ("Minio:SecretKey", _container.GetSecretKey()));
        return await Support.StartHostAsync(services => services.AddMinioBlobStoring(configuration));
    }

    protected override async Task StopAsync() => await Support.DisposeAsync(_container);
}
