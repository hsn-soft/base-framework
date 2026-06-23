using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.FeedRService.Application.Contracts.Events.Reporting;

public sealed record GenerateSummaryReportRequestedEto(Guid AppClientId,string DateRange) : IIntegrationEventMessage
{
    public Guid AppClientId { get; } = AppClientId;

    public string DateRange { get; } = DateRange;
}