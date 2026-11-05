using HsnSoft.Base.Domain.Entities.Events;

namespace Hsx.Shared.Contracts.Events.TextNormalizer;

/// <summary>The text normalizer took up a content: <paramref name="NormalizeRequestId"/> is its own record of the work. Published by the text normalizer; the content service keeps the id.</summary>
public sealed record CustomerContentNormalizeRequestCreatedEto(string ScopeKey, Guid CustomerContentId, Guid NormalizeRequestId) : IIntegrationEventMessage;
