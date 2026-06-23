using System.Diagnostics.CodeAnalysis;
using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.ContentService.Application.Contracts.Events;

public sealed record TrendVideoGenerationQueryEto([NotNull] string ScopeKey) : IIntegrationEventMessage
{
    [NotNull] public string ScopeKey { get; } = ScopeKey;
}