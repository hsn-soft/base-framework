namespace Hhs.Shared.Helper.Configuration.Providers;

/// <summary>
/// Base configuration for outline/script generation providers.
/// Default values represent reasonable defaults for content outline generation.
/// These can be overridden per provider via appsettings.json.
/// </summary>
public abstract class OutlineProviderSettingsBase : ProviderSettingsBase
{
    /// <summary>
    /// AI/LLM engine to use for outline generation. Default: "gpt-4"
    /// Provider-specific values depend on the implementation.
    /// Examples: "gpt-4", "gpt-3.5-turbo", "claude-3-opus", "llama-2"
    /// </summary>
    public string Engine { get; set; } = "gpt-4";

    /// <summary>
    /// Structure mode for outline generation. Default: "active"
    /// Controls how the content outline is structured.
    /// Examples: "active", "narrative", "chronological", "hierarchical"
    /// </summary>
    public string StructureMode { get; set; } = "active";
}
