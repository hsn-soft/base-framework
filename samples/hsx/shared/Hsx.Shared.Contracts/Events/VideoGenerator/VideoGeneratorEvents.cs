using HsnSoft.Base.Domain.Entities.Events;
using Hsx.Shared.Helper;

namespace Hsx.Shared.Contracts.Events.VideoGenerator;

/// <summary>The video generator took up the video of a content: <paramref name="VideoRequestId"/> is its own record of the work. Published by the video generator; the content service keeps the id.</summary>
public sealed record VideoRequestCreatedEto(string ScopeKey, Guid RefContentId, ContentTypes RefContentType, Guid VideoRequestId) : IIntegrationEventMessage;

/// <summary>The audio of a video is being made (<paramref name="Mode"/> is <c>VIDEO_AUDIO_INTERNAL</c>, the video provider makes it, or <c>VIDEO_AUDIO_EXTERNAL</c>, an audio provider makes it). Published by the video generator.</summary>
public sealed record AudioOperationStartedEto(string ScopeKey, Guid RefContentId, ContentTypes RefContentType, Guid VideoRequestId, string Mode) : IIntegrationEventMessage;

/// <summary>The video provider was asked for the video, with the audio files that are ready. Published by the video generator; the content service marks the video as being made.</summary>
public sealed record VideoProviderRequestStartedEto(string ScopeKey, Guid RefContentId, ContentTypes RefContentType, Guid VideoRequestId, IReadOnlyList<string> AudioUrls) : IIntegrationEventMessage;

/// <summary>The video of a content is made and is on the CDN. Published by the video generator; the content service keeps its address and, for an analysis, the pages it was made of.</summary>
public sealed record VideoGenerationResultPublishedEto(string ScopeKey, Guid RefContentId, ContentTypes RefContentType, Guid VideoRequestId, string VideoUrl, IReadOnlyList<VideoStoryEto> Stories) : IIntegrationEventMessage;

/// <summary>A page an analysis video was made of, as it is shown under the video.</summary>
public sealed record VideoStoryEto(Guid CustomerContentId, int SortOrder, string Title, string Description, string ImageUrl);
