namespace Hhs.VideoGeneratorService.Application.Contracts.JobDomain.Dtos;

public sealed record PollDueVideoRequestsTriggerDto(string JobName)
{
    public string JobName { get; } = JobName;
}
