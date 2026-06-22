namespace Hhs.MockApi.CdnLocalMinio.DTOs;

public sealed class UploadCdnFileRequest
{
    public string AssetType { get; set; } = "files";
    public IFormFile File { get; set; } = default!;
}
