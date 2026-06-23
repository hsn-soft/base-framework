namespace Hhs.ContentService.Application.Contracts.JobDomain.Dtos;

public sealed record TrendVideoGenerationQueryTriggerDto(string JobName)
{
    public string JobName { get; } = JobName;
}