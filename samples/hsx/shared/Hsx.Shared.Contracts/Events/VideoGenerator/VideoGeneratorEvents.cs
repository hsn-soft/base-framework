using HsnSoft.Base.Domain.Entities.Events;
using Hsx.Shared.Helper;

namespace Hsx.Shared.Contracts.Events.VideoGenerator;

/// <summary>The video of a content is made and is on the CDN. Published by the video generator; the content service keeps its address and, for an analysis, the pages it was made of.</summary>
public sealed record VideoGenerationResultPublishedEto(string ScopeKey, Guid RefContentId, ContentTypes RefContentType, Guid VideoRequestId, string VideoUrl, IReadOnlyList<VideoStoryEto> Stories) : IIntegrationEventMessage;

/// <summary>A page an analysis video was made of, as it is shown under the video.</summary>
public sealed record VideoStoryEto(Guid CustomerContentId, int SortOrder, string Title, string Description, string ImageUrl);
