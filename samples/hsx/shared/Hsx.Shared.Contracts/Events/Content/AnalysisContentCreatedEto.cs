using HsnSoft.Base.Domain.Entities.Events;

namespace Hsx.Shared.Contracts.Events.Content;

/// <summary>An analysis (the video of the most read pages of a day) was made, with its items in their order. Published by the content service; the text normalizer makes the text of each item.</summary>
public sealed record AnalysisContentCreatedEto(string ScopeKey, Guid AnalysisContentId, string DomainName, IReadOnlyList<AnalysisItemDto> Items) : IIntegrationEventMessage;
