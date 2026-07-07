namespace Hhs.FeedRService.Application.Contracts.JobDomain.Dtos;

public sealed record DerivePendingReportsTriggerDto(string JobName, int MaxCount, string JobPeriodDesc = null, DateTime? NextTriggerTimeUtc = null)
{
    public string JobName { get; } = JobName;

    public int MaxCount { get; } = MaxCount;

    public string JobPeriodDesc { get; } = JobPeriodDesc;

    public DateTime? NextTriggerTimeUtc { get; } = NextTriggerTimeUtc;
}
