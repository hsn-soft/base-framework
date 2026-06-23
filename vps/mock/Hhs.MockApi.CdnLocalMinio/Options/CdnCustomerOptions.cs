namespace Hhs.MockApi.CdnLocalMinio.Options;

public sealed class CdnCustomerOptions
{
    public string TenantKey { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
    public string RootPath { get; set; } = default!;
    public string BaseUrl { get; set; } = default!;
    public bool IsPublic { get; set; }
}
