using HsnSoft.Base.Domain.Entities.Events;
using Hsx.Shared.Helper;

namespace Hsx.Shared.Contracts.Events.Content;

/// <summary>The content passed the rules and a video is to be made of it. Published by the content service; the text normalizer hands its text to the video generator.</summary>
public sealed record VideoGenerationApprovedEto(string ScopeKey, Guid RefContentId, ContentTypes RefContentType) : IIntegrationEventMessage;
