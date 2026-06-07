using HsnSoft.Base.Domain.Entities.Events;
using JetBrains.Annotations;

namespace Hhs.Shared.Contracts.Events.TextNormalizer;

public sealed record AppContentNormalizedStartedEto(Guid TenantId, Guid ClientId, [NotNull] string DomainName, Guid AppContentId, [NotNull] string DomainPath) : IIntegrationEventMessage
{
    public Guid TenantId { get; } = TenantId;

    public Guid ClientId { get; } = ClientId;

    [NotNull]
    public string DomainName { get; } = DomainName;

    public Guid AppContentId { get; } = AppContentId;

    [NotNull]
    public string DomainPath { get; } = DomainPath;
}