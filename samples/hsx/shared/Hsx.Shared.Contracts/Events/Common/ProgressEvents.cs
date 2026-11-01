using HsnSoft.Base.Domain.Entities.Events;
using Hsx.Shared.Helper;

namespace Hsx.Shared.Contracts.Events.Common;

/// <summary>
/// A service took a step the content service keeps track of, and <paramref name="ReferenceId"/> is its own record of the work (the normalize request, the video request). Published by the text normalizer and the video generator;
/// <paramref name="Stage"/> is one of <see cref="ProgressStages"/>. One event in place of one for each step.
/// </summary>
public sealed record WorkProgressedEto(string ScopeKey, Guid RefContentId, ContentTypes RefContentType, string Stage, Guid ReferenceId) : IIntegrationEventMessage;

/// <summary>A step of the work failed. <paramref name="Retryable"/> says whether the service will try again (<see langword="false"/>: the work ended, and the content is marked failed).</summary>
public sealed record WorkFailedEto(string ScopeKey, Guid RefContentId, ContentTypes RefContentType, string Service, string Step, string Reason, bool Retryable) : IIntegrationEventMessage;

/// <summary>The stages of <see cref="WorkProgressedEto"/>.</summary>
public static class ProgressStages
{
    public const string NormalizeRequested = "normalize-requested";

    public const string ScrapingCompleted = "scraping-completed";

    public const string VideoRequested = "video-requested";

    public const string AudioStarted = "audio-started";

    public const string VideoProviderStarted = "video-provider-started";
}
