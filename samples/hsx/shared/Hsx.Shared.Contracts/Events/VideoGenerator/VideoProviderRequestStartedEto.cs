using HsnSoft.Base.Domain.Entities.Events;
using Hsx.Shared.Helper;

namespace Hsx.Shared.Contracts.Events.VideoGenerator;

/// <summary>The video provider was asked for the video, with the audio files that are ready. Published by the video generator; the content service marks the video as being made.</summary>
public sealed record VideoProviderRequestStartedEto(string ScopeKey, Guid RefContentId, ContentTypes RefContentType, Guid VideoRequestId, IReadOnlyList<string> AudioUrls) : IIntegrationEventMessage;
