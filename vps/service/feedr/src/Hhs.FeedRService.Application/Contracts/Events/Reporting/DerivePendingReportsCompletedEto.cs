using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.FeedRService.Application.Contracts.Events.Reporting;

public sealed record DerivePendingReportsCompletedEto(Guid AppClientId) : IIntegrationEventMessage
{
    public Guid AppClientId { get; } = AppClientId;
}