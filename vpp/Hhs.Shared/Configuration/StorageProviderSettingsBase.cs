namespace Hhs.Shared.Configuration;

public abstract class StorageProviderSettingsBase
{
    public string Type { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
    public string? SecretKey { get; set; }
    public string? BucketOrContainer { get; set; }
}
