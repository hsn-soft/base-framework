namespace Hhs.MockApi.CdnLocalMinio.DTOs;

public sealed class UploadCdnFileResponse
{
    public string ObjectKey { get; set; } = default!;
    public string CdnUrl { get; set; } = default!;
}
