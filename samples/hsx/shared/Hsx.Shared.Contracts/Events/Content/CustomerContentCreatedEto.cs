using HsnSoft.Base.Domain.Entities.Events;

namespace Hsx.Shared.Contracts.Events.Content;

/// <summary>A page of a customer's site was seen for the first time and is now a content. Published by the content service; the text normalizer makes its text.</summary>
public sealed record CustomerContentCreatedEto(string ScopeKey, Guid CustomerContentId, string DomainName, string DomainPath) : IIntegrationEventMessage;
