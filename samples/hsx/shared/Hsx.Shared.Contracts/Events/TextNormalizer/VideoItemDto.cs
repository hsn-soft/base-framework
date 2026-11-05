namespace Hsx.Shared.Contracts.Events.TextNormalizer;

/// <summary>One part of a video: the text that is read, and what is shown while it is.</summary>
public sealed record VideoItemDto(int SortOrder, Guid CustomerContentId, string Title, string ImageUrl, string Text);
