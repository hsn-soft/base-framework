namespace Hhs.AdministrationService.Application.Contracts.JobDomain.Dtos;

public sealed record SynchAllPermissionToCacheDbTriggerDto(string JobName)
{
    public string JobName { get; } = JobName;
}