using HsnSoft.Base.Domain.Entities.Events;
using JetBrains.Annotations;

namespace Hhs.Shared.Contracts.Events.TextNormalizer;

public sealed record AppContentNormalizedStartedEto([NotNull] string ScopeKey, [NotNull] string DomainName, Guid AppContentId, [NotNull] string DomainPath) : IIntegrationEventMessage
{
    [NotNull] public string ScopeKey { get; } = ScopeKey;

    [NotNull] public string DomainName { get; } = DomainName;

    public Guid AppContentId { get; } = AppContentId;

    [NotNull] public string DomainPath { get; } = DomainPath;
}