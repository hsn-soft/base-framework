using HsnSoft.Base.BlobStoring.Aws;
using Microsoft.Extensions.Configuration;

namespace HsnSoft.Base.IntegrationTests.BlobStoring;

/// <summary>The Amazon S3 client against MinIO, which speaks the S3 API: the same requests an AWS bucket gets.</summary>
[Collection(Collections.Minio)]
public sealed class AwsBlobTests(MinioFixture minio) : BlobStoreContractTests
{
    protected override async Task<IHost> StartAsync()
    {
        IConfiguration configuration = Support.Configuration(
            ("AwsS3:ServiceUrl", $"http://{minio.Container.Hostname}:{minio.Container.GetMappedPublicPort(9000)}"),
            ("AwsS3:AccessKey", minio.Container.GetAccessKey()),
            ("AwsS3:SecretKey", minio.Container.GetSecretKey()));
        return await Support.StartHostAsync(services => services.AddAwsBlobStoring(configuration));
    }

    protected override Task StopAsync() => Task.CompletedTask;
}
