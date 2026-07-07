namespace Hhs.FeedRService.Application.Contracts.JobDomain.Dtos;

public sealed record GenerateSummaryReportTriggerDto(string JobName, Guid ClientId, string DateRange, string JobPeriodDesc = null, DateTime? NextTriggerTimeUtc = null)
{
    public string JobName { get; } = JobName;

    public Guid ClientId { get; } = ClientId;

    public string DateRange { get; } = DateRange;

    public string JobPeriodDesc { get; } = JobPeriodDesc;

    public DateTime? NextTriggerTimeUtc { get; } = NextTriggerTimeUtc;
}
