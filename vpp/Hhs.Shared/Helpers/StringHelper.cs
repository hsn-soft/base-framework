namespace Hhs.Shared.Helpers;

public static class StringHelper
{
    /// <summary>
    /// Converts a content key to a slug format.
    /// Example: "/haber/gundem/21" → "haber-gundem-21"
    /// TODO: This is a temporary implementation. Replace with proper slugification logic.
    /// </summary>
    public static string ToSlug(string contentKey)
    {
        if (string.IsNullOrWhiteSpace(contentKey))
            return string.Empty;

        // TODO: Implement proper slug conversion
        // Current: Simple placeholder - converts to lowercase and replaces / with -
        return contentKey
            .Trim('/')
            .ToLowerInvariant()
            .Replace("/", "-")
            .Replace(" ", "-");
    }
}
