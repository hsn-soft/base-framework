namespace Hhs.MockApi.CdnLocalMinio.Options;

public sealed class CdnCustomerOptions
{
    public string CustomerKey { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
    public string RootPath { get; set; } = default!;
    public string BaseUrl { get; set; } = default!;
    public bool IsPublic { get; set; }
}
