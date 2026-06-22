using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Hhs.MockApi.CdnLocalMinio.Options;

namespace Hhs.MockApi.CdnLocalMinio.Controllers;

[ApiController]
[Route("{**path}")]
public sealed class PublicCdnController(
    IMinioClient minioClient,
    IOptions<MinioOptions> minioOptions,
    IOptions<List<CdnCustomerOptions>> customers,
    FileExtensionContentTypeProvider contentTypeProvider)
    : ControllerBase
{
    private readonly MinioOptions _minioOptions = minioOptions.Value;
    private readonly List<CdnCustomerOptions> _customers = customers.Value;

    [HttpGet]
    public async Task<IActionResult> GetPublicFile(string path, CancellationToken cancellationToken)
    {
        var customer = _customers.FirstOrDefault(x => path.StartsWith(x.RootPath));

        if (customer is null)
            return NotFound(new { error = "Customer not found" });

        if (!customer.IsPublic)
            return Unauthorized(new { error = "This customer's content is not public" });

        var objectKey = $"{customer.TenantKey}/{path}";

        var memoryStream = new MemoryStream();

        try
        {
            var getArgs = new GetObjectArgs()
                .WithBucket(_minioOptions.BucketName)
                .WithObject(objectKey)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memoryStream);
                });

            await minioClient.GetObjectAsync(getArgs, cancellationToken);
        }
        catch
        {
            return NotFound(new { error = "File not found" });
        }

        memoryStream.Position = 0;

        if (!contentTypeProvider.TryGetContentType(path, out var contentType))
            contentType = "application/octet-stream";

        Response.Headers.CacheControl = "public, max-age=31536000, immutable";

        return File(memoryStream, contentType);
    }
}
