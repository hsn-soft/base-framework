namespace Hhs.Shared.Configuration;

public abstract class OutlineProviderSettings : ProviderSettingsBase
{
    public string Engine { get; set; } = "gpt-4";
    public string StructureMode { get; set; } = "active";
}
