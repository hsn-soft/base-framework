namespace Hhs.VideoGeneratorService.Application.Contracts.JobDomain.Dtos;

public sealed record VideoRequestQueryTriggerDto(string JobName)
{
    public string JobName { get; } = JobName;
}