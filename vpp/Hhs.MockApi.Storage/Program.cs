using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

var builder = WebApplication.CreateBuilder(args);

// Configure Options
builder.Services.Configure<MinioOptions>(
    builder.Configuration.GetSection("Minio"));

builder.Services.Configure<List<StorageCustomerOptions>>(
    builder.Configuration.GetSection("StorageCustomers"));

// Register Minio Client
builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var opt = builder.Configuration.GetSection("Minio").Get<MinioOptions>()!;

    return new MinioClient()
        .WithEndpoint(opt.Endpoint)
        .WithCredentials(opt.AccessKey, opt.SecretKey)
        .WithSSL(opt.UseSsl)
        .Build();
});

// Register Services
builder.Services.AddSingleton<FileExtensionContentTypeProvider>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.Run();

// ============================================================================
// OPTIONS CLASSES
// ============================================================================

public sealed class MinioOptions
{
    public string Endpoint { get; set; } = default!;
    public string AccessKey { get; set; } = default!;
    public string SecretKey { get; set; } = default!;
    public string BucketName { get; set; } = default!;
    public bool UseSsl { get; set; }
}

public sealed class StorageCustomerOptions
{
    public string CustomerKey { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
    public string RootPath { get; set; } = default!;
    public bool IsPublic { get; set; }
}

// ============================================================================
// REQUEST/RESPONSE DTOs
// ============================================================================

public sealed class UploadStorageFileRequest
{
    public string FileType { get; set; } = "files";
    public IFormFile File { get; set; } = default!;
}

public sealed class UploadStorageFileResponse
{
    public string ObjectKey { get; set; } = default!;
}

// ============================================================================
// CONTROLLERS
// ============================================================================

[ApiController]
[Route("api/storage")]
public sealed class StorageController : ControllerBase
{
    private readonly IMinioClient _minioClient;
    private readonly MinioOptions _minioOptions;
    private readonly List<StorageCustomerOptions> _customers;

    public StorageController(
        IMinioClient minioClient,
        IOptions<MinioOptions> minioOptions,
        IOptions<List<StorageCustomerOptions>> customers)
    {
        _minioClient = minioClient;
        _minioOptions = minioOptions.Value;
        _customers = customers.Value;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        [FromForm] UploadStorageFileRequest request,
        CancellationToken cancellationToken)
    {
        var customer = ResolveCustomer();
        if (customer is null)
            return Unauthorized(new { error = "API key is required." });

        if (request.File.Length == 0)
            return BadRequest(new { error = "File is empty." });

        var safeFileName = Path.GetFileName(request.File.FileName);
        var fileType = string.IsNullOrWhiteSpace(request.FileType) ? "files" : request.FileType;

        var objectKey =
            $"{customer.RootPath}/{fileType}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}-{safeFileName}";

        await using var stream = request.File.OpenReadStream();

        var putArgs = new PutObjectArgs()
            .WithBucket(_minioOptions.BucketName)
            .WithObject(objectKey)
            .WithStreamData(stream)
            .WithObjectSize(request.File.Length)
            .WithContentType(request.File.ContentType ?? "application/octet-stream");

        await _minioClient.PutObjectAsync(putArgs, cancellationToken);

        return Ok(new UploadStorageFileResponse
        {
            ObjectKey = objectKey
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

            await _minioClient.GetObjectAsync(getArgs, cancellationToken);
        }
        catch
        {
            return NotFound(new { error = "File not found in storage." });
        }

        memoryStream.Position = 0;

        var fileName = Path.GetFileName(key);
        return File(memoryStream, "application/octet-stream", fileName);
    }

    private StorageCustomerOptions? ResolveCustomer()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
            return null;

        return _customers.FirstOrDefault(x => x.ApiKey == apiKey.ToString());
    }
}
