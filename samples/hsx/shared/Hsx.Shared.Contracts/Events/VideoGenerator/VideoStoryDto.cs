namespace Hsx.Shared.Contracts.Events.VideoGenerator;

/// <summary>A page an analysis video was made of, as it is shown under the video.</summary>
public sealed record VideoStoryDto(Guid CustomerContentId, int SortOrder, string Title, string Description, string ImageUrl);
