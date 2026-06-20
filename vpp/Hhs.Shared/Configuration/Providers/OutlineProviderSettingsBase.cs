namespace Hhs.Shared.Configuration.Providers;

public abstract class OutlineProviderSettingsBase : ProviderSettingsBase
{
    public string Engine { get; set; } = "gpt-4";
    public string StructureMode { get; set; } = "active";
}
