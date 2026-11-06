using HsnSoft.Base.Domain.Entities.Events;
using Hsx.Shared.Helper;

namespace Hsx.Shared.Contracts.Events.VideoGenerator;

/// <summary>The video generator took up the video of a content: <paramref name="VideoRequestId"/> is its own record of the work. Published by the video generator; the content service keeps the id.</summary>
public sealed record VideoRequestCreatedEto(string ScopeKey, Guid RefContentId, ContentTypes RefContentType, Guid VideoRequestId) : IIntegrationEventMessage;
