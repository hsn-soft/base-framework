namespace Hhs.VideoGeneratorService.Application.Contracts.JobDomain.Dtos;

public sealed record AdvanceReadyVideoRequestsTriggerDto(string JobName, string JobPeriodDesc = null, DateTime? NextTriggerTimeUtc = null)
{
    public string JobName { get; } = JobName;
    public string JobPeriodDesc { get; } = JobPeriodDesc;
    public DateTime? NextTriggerTimeUtc { get; } = NextTriggerTimeUtc;
}
