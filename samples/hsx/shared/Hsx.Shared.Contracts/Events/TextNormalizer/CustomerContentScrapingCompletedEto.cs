using HsnSoft.Base.Domain.Entities.Events;

namespace Hsx.Shared.Contracts.Events.TextNormalizer;

/// <summary>The page of a content was read: <paramref name="ReleaseTimeUtc"/> is when it was published, as the page says (the content service decides with it whether a video is made). Published by the text normalizer.</summary>
public sealed record CustomerContentScrapingCompletedEto(string ScopeKey, Guid CustomerContentId, DateTime? ReleaseTimeUtc) : IIntegrationEventMessage;
