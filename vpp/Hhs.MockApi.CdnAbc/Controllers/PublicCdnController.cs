using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Hhs.MockApi.CdnAbc.Options;

namespace Hhs.MockApi.CdnAbc.Controllers;

[ApiController]
[Route("{**path}")]
public sealed class PublicCdnController : ControllerBase
{
    private readonly StorageApiOptions _storageApiOptions;
    private readonly List<CdnCustomerOptions> _customers;
    private readonly IHttpClientFactory _httpClientFactory;

    public PublicCdnController(
        IOptions<StorageApiOptions> storageApiOptions,
        IOptions<List<CdnCustomerOptions>> customers,
        IHttpClientFactory httpClientFactory)
    {
        _storageApiOptions = storageApiOptions.Value;
        _customers = customers.Value;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetPublicFile(string path, CancellationToken cancellationToken)
    {
        var customer = _customers.FirstOrDefault(x => path.StartsWith(x.RootPath));

        if (customer is null)
            return NotFound(new { error = "Customer not found" });

        if (!customer.IsPublic)
            return Unauthorized(new { error = "This customer's content is not public" });

        var objectKey = $"{customer.TenantKey}/{path}";

        var httpClient = _httpClientFactory.CreateClient();
        var storageRequest = new HttpRequestMessage(HttpMethod.Get, $"{_storageApiOptions.BaseUrl.TrimEnd('/')}/api/storage/download?key={Uri.EscapeDataString(objectKey)}");
        storageRequest.Headers.Add("X-Api-Key", _storageApiOptions.ApiKey);

        var downloadResponse = await httpClient.SendAsync(storageRequest, cancellationToken);

        if (!downloadResponse.IsSuccessStatusCode)
        {
            return NotFound(new { error = "File not found" });
        }

        var fileStream = await downloadResponse.Content.ReadAsStreamAsync(cancellationToken);
        var contentType = "application/octet-stream";
        var fileName = Path.GetFileName(path);

        Response.Headers.CacheControl = "public, max-age=31536000, immutable";

        return File(fileStream, contentType);
    }
}
