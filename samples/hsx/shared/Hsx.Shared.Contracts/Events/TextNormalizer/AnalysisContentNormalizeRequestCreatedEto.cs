using HsnSoft.Base.Domain.Entities.Events;

namespace Hsx.Shared.Contracts.Events.TextNormalizer;

/// <summary>The text normalizer took up an analysis. Published by the text normalizer; the content service keeps the id.</summary>
public sealed record AnalysisContentNormalizeRequestCreatedEto(string ScopeKey, Guid AnalysisContentId, Guid NormalizeRequestId) : IIntegrationEventMessage;
