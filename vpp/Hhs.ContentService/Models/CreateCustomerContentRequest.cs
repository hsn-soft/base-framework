namespace Hhs.ContentService;

public sealed record CreateCustomerContentRequest(
    string ScopeKey,
    string DomainName,
    string ContentKey,
    string? OutlineProviderKey = "openai",
    string? VideoProviderKey = "video-external",
    string? AudioProviderKey = "audio-def");