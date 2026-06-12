using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.FeedRService.Application.Contracts.Events.Reporting;

public sealed record DerivePendingReportsRequestedEto(int MaxCount) : IIntegrationEventMessage
{
    public int MaxCount { get; } = MaxCount;
}