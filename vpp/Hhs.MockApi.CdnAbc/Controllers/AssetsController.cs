using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Hhs.MockApi.CdnAbc.Options;

namespace Hhs.MockApi.CdnAbc.Controllers;

[ApiController]
[Route("api/cdn/assets")]
public sealed class AssetsController : ControllerBase
{
    private readonly StorageApiOptions _storageApiOptions;
    private readonly List<CdnCustomerOptions> _customers;
    private readonly IHttpClientFactory _httpClientFactory;

    public AssetsController(
        IOptions<StorageApiOptions> storageApiOptions,
        IOptions<List<CdnCustomerOptions>> customers,
        IHttpClientFactory httpClientFactory)
    {
        _storageApiOptions = storageApiOptions.Value;
        _customers = customers.Value;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] UploadCdnFileRequest request, CancellationToken cancellationToken)
    {
        var customer = ResolveCustomer();
        if (customer is null)
            return Unauthorized(new { error = "Invalid API key." });

        if (request.File.Length == 0)
            return BadRequest(new { error = "File is empty." });

        var safeFileName = Path.GetFileName(request.File.FileName);
        var objectKey = $"{customer.TenantKey}/{customer.RootPath}/{DateTime.UtcNow:yyyy-MM-dd}-{Guid.NewGuid():N}-{safeFileName}";

        using var formContent = new MultipartFormDataContent();
        formContent.Add(request.File.OpenReadStream(), "file", safeFileName);

        var httpClient = _httpClientFactory.CreateClient();
        var storageRequest = new HttpRequestMessage(HttpMethod.Post, $"{_storageApiOptions.BaseUrl.TrimEnd('/')}/api/storage/upload")
        {
            Content = formContent
        };
        storageRequest.Headers.Add("X-Api-Key", _storageApiOptions.ApiKey);

        var uploadResponse = await httpClient.SendAsync(storageRequest, cancellationToken);

        if (!uploadResponse.IsSuccessStatusCode)
        {
            return BadRequest(new { error = $"Storage upload failed: {uploadResponse.StatusCode}" });
        }

        var relativePath = objectKey.Substring(objectKey.IndexOf(customer.RootPath));
        var cdnUrl = $"{customer.BaseUrl}/{relativePath}";

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

        var httpClient = _httpClientFactory.CreateClient();
        var storageRequest = new HttpRequestMessage(HttpMethod.Get, $"{_storageApiOptions.BaseUrl.TrimEnd('/')}/api/storage/download?key={Uri.EscapeDataString(key)}");
        storageRequest.Headers.Add("X-Api-Key", _storageApiOptions.ApiKey);

        var downloadResponse = await httpClient.SendAsync(storageRequest, cancellationToken);

        if (!downloadResponse.IsSuccessStatusCode)
        {
            return NotFound(new { error = "File not found in storage." });
        }

        var fileStream = await downloadResponse.Content.ReadAsStreamAsync(cancellationToken);
        var fileName = Path.GetFileName(key);
        return File(fileStream, "application/octet-stream", fileName);
    }

    private CdnCustomerOptions? ResolveCustomer()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
            return null;

        return _customers.FirstOrDefault(x => x.ApiKey == apiKey.ToString());
    }
}

public sealed class UploadCdnFileRequest
{
    public IFormFile File { get; set; } = default!;
}

public sealed class UploadCdnFileResponse
{
    public string ObjectKey { get; set; } = default!;
    public string CdnUrl { get; set; } = default!;
}
