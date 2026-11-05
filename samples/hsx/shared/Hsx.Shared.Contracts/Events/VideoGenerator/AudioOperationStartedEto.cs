using HsnSoft.Base.Domain.Entities.Events;
using Hsx.Shared.Helper;

namespace Hsx.Shared.Contracts.Events.VideoGenerator;

/// <summary>The audio of a video is being made (<paramref name="Mode"/> is <c>VIDEO_AUDIO_INTERNAL</c>, the video provider makes it, or <c>VIDEO_AUDIO_EXTERNAL</c>, an audio provider makes it). Published by the video generator.</summary>
public sealed record AudioOperationStartedEto(string ScopeKey, Guid RefContentId, ContentTypes RefContentType, Guid VideoRequestId, string Mode) : IIntegrationEventMessage;
