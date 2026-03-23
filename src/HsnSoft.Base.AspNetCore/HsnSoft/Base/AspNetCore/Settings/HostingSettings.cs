namespace HsnSoft.Base.AspNetCore.Settings;

public sealed class HostingSettings
{
    public bool IsEnabledRequestResponseLogger { get; set; } = true;
    public bool IsEnabledHealthCheckRequestLogger { get; set; } = false;

    public int MaxLoggedRequestBodySizeBytes { get; set; } = 8 * 1024;
    public int MaxLoggedResponseBodySizeBytes { get; set; } = 8 * 1024;
    public int MaxLoggedHeaderValueLength { get; set; } = 128;
}