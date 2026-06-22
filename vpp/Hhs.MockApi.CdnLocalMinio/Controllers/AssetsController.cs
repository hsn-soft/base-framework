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
        if (customer is null)
            return Unauthorized(new { error = "Invalid API key." });

        if (request.File.Length == 0)
            return BadRequest(new { error = "File is empty." });

        var safeFileName = Path.GetFileName(request.File.FileName);

        var objectKey =
            $"{customer.TenantKey}/{customer.RootPath}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}-{safeFileName}";

        await using var stream = request.File.OpenReadStream();

        var putArgs = new PutObjectArgs()
            .WithBucket(_minioOptions.BucketName)
            .WithObject(objectKey)
            .WithStreamData(stream)
            .WithObjectSize(request.File.Length)
            .WithContentType(request.File.ContentType ?? "application/octet-stream");

        await minioClient.PutObjectAsync(putArgs, cancellationToken);

        var relativePath = objectKey.Substring(objectKey.IndexOf(customer.RootPath));
        var cdnUrl = $"{customer.BaseUrl}/{relativePath}";
        var storageUrl = $"{Request.Scheme}://{Request.Host}/api/cdn/assets/download?key={Uri.EscapeDataString(objectKey)}";

        return Ok(new UploadCdnFileResponse
        {
            ObjectKey = objectKey,
            CdnUrl = cdnUrl
        });
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download([FromQuery] string key, CancellationToken cancellationToken)
    {
        var customer = ResolveCustomer();
        if (customer is null)
            return Unauthorized(new { error = "API key is required." });

        if (string.IsNullOrEmpty(key))
            return BadRequest(new { error = "Object key is required." });

        var memoryStream = new MemoryStream();

        try
        {
            var getArgs = new GetObjectArgs()
                .WithBucket(_minioOptions.BucketName)
                .WithObject(key)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memoryStream);
                });

            await minioClient.GetObjectAsync(getArgs, cancellationToken);
        }
        catch
        {
            return NotFound(new { error = "File not found in storage." });
        }

        memoryStream.Position = 0;

        var fileName = Path.GetFileName(key);
        return File(memoryStream, "application/octet-stream", fileName);
    }

    private CdnCustomerOptions? ResolveCustomer()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
            return null;

        return _customers.FirstOrDefault(x => x.ApiKey == apiKey.ToString());
    }
}
