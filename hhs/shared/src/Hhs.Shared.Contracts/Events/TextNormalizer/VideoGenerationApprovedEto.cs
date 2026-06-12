using System.Diagnostics.CodeAnalysis;
using Hhs.Shared.Helper.Enums;
using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.Shared.Contracts.Events.TextNormalizer;

public sealed record VideoGenerationApprovedEto([NotNull] string ScopeKey, ReferenceContentTypes ReferenceContentType, Guid ReferenceContentId) : IIntegrationEventMessage
{
    [NotNull] public string ScopeKey { get; } = ScopeKey;

    public ReferenceContentTypes ReferenceContentType { get; } = ReferenceContentType;
    public Guid ReferenceContentId { get; } = ReferenceContentId;
}