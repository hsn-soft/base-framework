namespace Hhs.TextNormalizerService.Application.Contracts.JobDomain.Dtos;

public sealed record PollDueOutlineRequestsTriggerDto(string JobName)
{
    public string JobName { get; } = JobName;
}
