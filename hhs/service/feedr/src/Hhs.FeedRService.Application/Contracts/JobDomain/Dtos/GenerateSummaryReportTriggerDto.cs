namespace Hhs.FeedRService.Application.Contracts.JobDomain.Dtos;

public sealed record GenerateSummaryReportTriggerDto(string JobName, Guid ClientId, string DateRange)
{
    public string JobName { get; } = JobName;

    public Guid ClientId { get; } = ClientId;

    public string DateRange { get; } = DateRange;
}