using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Hhs.MockApi.CdnLocalMinio.Options;

namespace Hhs.MockApi.CdnLocalMinio.Controllers;

[ApiController]
[Route("{customerKey}/{**path}")]
public sealed class PublicCdnController : ControllerBase
{
    private readonly IMinioClient _minioClient;
    private readonly MinioOptions _minioOptions;
    private readonly List<CdnCustomerOptions> _customers;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;

    public PublicCdnController(
        IMinioClient minioClient,
        IOptions<MinioOptions> minioOptions,
        IOptions<List<CdnCustomerOptions>> customers,
        FileExtensionContentTypeProvider contentTypeProvider)
    {
        _minioClient = minioClient;
        _minioOptions = minioOptions.Value;
        _customers = customers.Value;
        _contentTypeProvider = contentTypeProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetPublicFile(
        string customerKey,
        string path,
        CancellationToken cancellationToken)
    {
        var customer = _customers.FirstOrDefault(x =>
            x.CustomerKey.Equals(customerKey, StringComparison.OrdinalIgnoreCase));

        if (customer is null)
            return NotFound(new { error = "Customer not found" });

        if (!customer.IsPublic)
            return Unauthorized(new { error = "This customer's content is not public" });

        var objectKey = $"{customer.RootPath}/{path}";

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

            await _minioClient.GetObjectAsync(getArgs, cancellationToken);
        }
        catch
        {
            return NotFound(new { error = "File not found" });
        }

        memoryStream.Position = 0;

        if (!_contentTypeProvider.TryGetContentType(path, out var contentType))
            contentType = "application/octet-stream";

        Response.Headers.CacheControl = "public, max-age=31536000, immutable";

        return File(memoryStream, contentType);
    }
}
