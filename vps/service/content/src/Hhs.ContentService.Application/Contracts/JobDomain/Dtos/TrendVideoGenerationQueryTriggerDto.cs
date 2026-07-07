namespace Hhs.ContentService.Application.Contracts.JobDomain.Dtos;

public sealed record TrendVideoGenerationQueryTriggerDto(string JobName, string JobPeriodDesc = null, DateTime? NextTriggerTimeUtc = null)
{
    public string JobName { get; } = JobName;
    public string JobPeriodDesc { get; } = JobPeriodDesc;
    public DateTime? NextTriggerTimeUtc { get; } = NextTriggerTimeUtc;
}
