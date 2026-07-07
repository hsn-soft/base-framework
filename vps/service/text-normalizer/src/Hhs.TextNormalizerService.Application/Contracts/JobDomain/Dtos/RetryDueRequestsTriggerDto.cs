namespace Hhs.TextNormalizerService.Application.Contracts.JobDomain.Dtos;

public sealed record RetryDueRequestsTriggerDto(string JobName)
{
    public string JobName { get; } = JobName;
}
