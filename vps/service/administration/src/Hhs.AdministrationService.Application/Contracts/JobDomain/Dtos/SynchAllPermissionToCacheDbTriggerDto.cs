namespace Hhs.AdministrationService.Application.Contracts.JobDomain.Dtos;

public sealed record SynchAllPermissionToCacheDbTriggerDto(string JobName, string JobPeriodDesc = null, DateTime? NextTriggerTimeUtc = null)
{
    public string JobName { get; } = JobName;
    public string JobPeriodDesc { get; } = JobPeriodDesc;
    public DateTime? NextTriggerTimeUtc { get; } = NextTriggerTimeUtc;
}
