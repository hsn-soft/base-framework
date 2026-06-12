namespace Hhs.FeedRService.Application.Contracts.JobDomain.Dtos;

public sealed record DerivePendingReportsTriggerDto(string JobName, int MaxCount)
{
    public string JobName { get; } = JobName;

    public int MaxCount { get; } = MaxCount;
}