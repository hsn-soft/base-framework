namespace Hhs.VideoGeneratorService.Application.Contracts.JobDomain.Dtos;

public sealed record PollDueAudioRequestsTriggerDto(string JobName)
{
    public string JobName { get; } = JobName;
}
