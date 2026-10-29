using HsnSoft.Base.BlobStoring.Aws;
using Microsoft.Extensions.Configuration;
using Testcontainers.Minio;

namespace HsnSoft.Base.IntegrationTests.BlobStoring;

/// <summary>The Amazon S3 client against MinIO, which speaks the S3 API: the same requests an AWS bucket gets.</summary>
public sealed class AwsBlobTests : BlobStoreContractTests
{
    private readonly MinioContainer _container = new MinioBuilder("minio/minio:RELEASE.2024-08-29T01-40-52Z").Build();

    protected override async Task<IHost> StartAsync()
    {
        await _container.StartAsync();
        IConfiguration configuration = Support.Configuration(
            ("AwsS3:ServiceUrl", $"http://{_container.Hostname}:{_container.GetMappedPublicPort(9000)}"),
            ("AwsS3:AccessKey", _container.GetAccessKey()),
            ("AwsS3:SecretKey", _container.GetSecretKey()));
        return await Support.StartHostAsync(services => services.AddAwsBlobStoring(configuration));
    }

    protected override async Task StopAsync() => await Support.DisposeAsync(_container);
}
