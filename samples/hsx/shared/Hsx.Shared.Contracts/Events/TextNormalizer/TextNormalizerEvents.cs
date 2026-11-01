using HsnSoft.Base.Domain.Entities.Events;
using Hsx.Shared.Helper;

namespace Hsx.Shared.Contracts.Events.TextNormalizer;

/// <summary>
/// The text of a content is ready, or it was left out (<paramref name="Skipped"/>: the page is too old to make a video of). Published by the text normalizer; the content service decides, with its rules, whether a video is made.
/// <paramref name="ReleaseTimeUtc"/> is when the page was published, as the page says.
/// </summary>
public sealed record NormalizerResultPublishedEto(string ScopeKey, Guid RefContentId, ContentTypes RefContentType, bool Skipped, DateTime? ReleaseTimeUtc) : IIntegrationEventMessage;

/// <summary>The text of an approved content, in the order of its video. Published by the text normalizer; the video generator makes the audio and the video of it.</summary>
public sealed record VideoGenerationDataForwardedEto(string ScopeKey, Guid RefContentId, ContentTypes RefContentType, IReadOnlyList<VideoItemEto> Items) : IIntegrationEventMessage;

/// <summary>One part of a video: the text that is read, and what is shown while it is.</summary>
public sealed record VideoItemEto(int SortOrder, Guid CustomerContentId, string Title, string ImageUrl, string Text);
