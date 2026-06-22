using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Hhs.MockApi.CdnLocalMinio.Options;
using Hhs.MockApi.CdnLocalMinio.DTOs;

namespace Hhs.MockApi.CdnLocalMinio.Controllers;

[ApiController]
[Route("api/cdn/assets")]
public sealed class AssetsController(
    IMinioClient minioClient,
    IOptions<MinioOptions> minioOptions,
    IOptions<List<CdnCustomerOptions>> customers) : ControllerBase
{
    private readonly MinioOptions _minioOptions = minioOptions.Value;
    private readonly List<CdnCustomerOptions> _customers = customers.Value;

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] UploadCdnFileRequest request, CancellationToken cancellationToken)
    {
        var customer = ResolveCustomer();

        if (request.File.Length == 0)
            return BadRequest(new { error = "File is empty." });

        var safeFileName = Path.GetFileName(request.File.FileName);
        var assetType = string.IsNullOrWhiteSpace(request.AssetType) ? "files" : request.AssetType;

        var objectKey =
            $"{customer.RootPath}/{assetType}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}-{safeFileName}";

        await using var stream = request.File.OpenReadStream();

        var putArgs = new PutObjectArgs()
            .WithBucket(_minioOptions.BucketName)
            .WithObject(objectKey)
            .WithStreamData(stream)
            .WithObjectSize(request.File.Length)
            .WithContentType(request.File.ContentType ?? "application/octet-stream");

        await minioClient.PutObjectAsync(putArgs, cancellationToken);

        var relativePath = objectKey.Replace(customer.RootPath + "/", "");
        var cdnUrl = $"{customer.BaseUrl}/{relativePath}";

        return Ok(new UploadCdnFileResponse
        {
            CdnUrl = cdnUrl,
            ObjectKey = objectKey
        });
    }

    private CdnCustomerOptions ResolveCustomer()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
            throw new UnauthorizedAccessException("API key is required.");

        var customer = _customers.FirstOrDefault(x => x.ApiKey == apiKey.ToString());

        if (customer is null)
            throw new UnauthorizedAccessException("Invalid API key.");

        return customer;
    }
}
