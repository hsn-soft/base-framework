using Hhs.Shared.Helper.Configuration.Providers;

namespace Hhs.TextNormalizerService.Domain.Configuration.Providers.Outline;

public sealed class OutlineOpenAiProviderSettings : OutlineProviderSettingsBase
{
    public const string SectionName = "Provider:Outline:OutlineOpenAi";

    // Engine used for structured output mode (supports JSON schema / function calling)
    public string StructuredEngine { get; set; } = "gpt-4o-mini";
}
