namespace Hhs.FeedRService.Application.Contracts.JobDomain.Dtos;

public sealed record TestQueryTriggerDto(string JobName, Guid AppClientId, string JobPeriodDesc = null, DateTime? NextTriggerTimeUtc = null)
{
    public string JobName { get; } = JobName;

    public Guid AppClientId { get; } = AppClientId;

    public string JobPeriodDesc { get; } = JobPeriodDesc;

    public DateTime? NextTriggerTimeUtc { get; } = NextTriggerTimeUtc;
}
