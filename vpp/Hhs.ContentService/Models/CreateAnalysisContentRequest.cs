namespace Hhs.ContentService;

public sealed record CreateAnalysisContentRequest(
    string ScopeKey,
    string DomainName,
    string Title,
    List<Guid> CustomerContentIds,
    string OutlineProviderKey = "openai",
    string VideoProviderKey = "video-a",
    string? AudioProviderKey = "audio-a");