using HsnSoft.Base.Domain.Entities.Events;
using JetBrains.Annotations;

namespace Hhs.Shared.Contracts.Events.TextNormalizer;

public sealed record AppContentNormalizedStartedEto([NotNull] string ScopeKey, [NotNull] string DomainName, Guid CustomerContentId, [NotNull] string DomainPath) : IIntegrationEventMessage
{
    [NotNull] public string ScopeKey { get; } = ScopeKey;

    [NotNull] public string DomainName { get; } = DomainName;

    public Guid CustomerContentId { get; } = CustomerContentId;

    [NotNull] public string DomainPath { get; } = DomainPath;
}