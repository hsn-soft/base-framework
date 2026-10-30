using HsnSoft.Base.BlobStoring.Minio;
using Microsoft.Extensions.Configuration;

namespace HsnSoft.Base.IntegrationTests.BlobStoring;

[Collection(Collections.Minio)]
public sealed class MinioTests(MinioFixture minio) : BlobStoreContractTests
{
    protected override async Task<IHost> StartAsync()
    {
        IConfiguration configuration = Support.Configuration(
            ("Minio:Endpoint", $"{minio.Container.Hostname}:{minio.Container.GetMappedPublicPort(9000)}"),
            ("Minio:AccessKey", minio.Container.GetAccessKey()),
            ("Minio:SecretKey", minio.Container.GetSecretKey()));
        return await Support.StartHostAsync(services => services.AddMinioBlobStoring(configuration));
    }

    protected override Task StopAsync() => Task.CompletedTask;
}
