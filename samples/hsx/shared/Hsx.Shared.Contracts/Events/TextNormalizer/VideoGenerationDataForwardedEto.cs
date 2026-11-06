using HsnSoft.Base.Domain.Entities.Events;
using Hsx.Shared.Helper;

namespace Hsx.Shared.Contracts.Events.TextNormalizer;

/// <summary>The text of an approved content, in the order of its video. Published by the text normalizer; the video generator makes the audio and the video of it.</summary>
public sealed record VideoGenerationDataForwardedEto(string ScopeKey, Guid RefContentId, ContentTypes RefContentType, IReadOnlyList<VideoItemDto> Items) : IIntegrationEventMessage;
